using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance;
    public GameObject pickupPrefab;

    [SyncVar]
    double endTimestamp;

    public List<uint> allPlayers = new List<uint>();
    public readonly SyncList<uint> allPlayerIds = new SyncList<uint>();
    List<uint> matchPlayers = new List<uint>();

    [SyncVar(hook = nameof(OnRoomStateChanged))]
    public RoomState currentState;

    [SyncVar(hook = nameof(OnPrepareEndChanged))]
    double prepareEndTimestamp;

    [SyncVar(hook = nameof(OnGenerateEndChanged))]
    double generateEndTimestamp;

    [SyncVar(hook = nameof(OnMapSeedChanged))]
    public long currentMapSeed = 0;

    [SyncVar]
    public bool hasMapSeed = false;

    public Transform[] spawnPoints;



    [Header("准备阶段时长")]
    public float prepareTime = 10f;
    [Header("地图生成阶段时长")]
    public float generateTime = 3f;
    [Header("游戏结算时长")]
    public float settleTime = 5f;

    bool preparingStarted;
    bool settling;
    Coroutine matchFlowRoutine;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnEnable()
    {
        if (Instance == null)
            Instance = this;

        allPlayerIds.Callback += OnPlayerListChanged;
    }

    void OnDisable()
    {
        allPlayerIds.Callback -= OnPlayerListChanged;
    }

    void OnPlayerListChanged(SyncList<uint>.Operation op, int index, uint oldItem, uint newItem)
    {
        if (!isClient) return;

        StartCoroutine(DelayUI());
    }

    IEnumerator DelayUI()
    {
        yield return null; // 等 spawn 完成
        ApplyPlayerListToUI();
    }

    void OnMapSeedChanged(long oldValue, long newValue)
    {
        if (!isClient) return;

        // 有些情况下 seed 还没生效或未生成
        if (!hasMapSeed) return;

        WallVisibilityController.Instance?.ApplySeed(newValue);
    }

    public PlayerController GetPlayer(uint netId)
    {
        if (isServer)
        {
            if (NetworkServer.spawned.TryGetValue(netId, out var id))
                return id.GetComponent<PlayerController>();
        }
        else
        {
            if (NetworkClient.spawned.TryGetValue(netId, out var id))
                return id.GetComponent<PlayerController>();
        }

        return null;
    }

    void OnPrepareEndChanged(double oldValue, double newValue)
    {
        if (!isClient) return;

        RoomCanvasController.Instance?.SetPrepareEnd(newValue);
    }

    void OnGenerateEndChanged(double oldValue, double newValue)
    {
        if (!isClient) return;

        RoomCanvasController.Instance?.SetGenerateEnd(newValue);
    }

    void ApplyPlayerListToUI()
    {
        if (RoomCanvasController.Instance == null)
            return;

        if (currentState != RoomState.Waiting &&
            currentState != RoomState.Preparing &&
            currentState != RoomState.Generating)
            return;

        List<PlayerController> list = new List<PlayerController>();

        foreach (uint id in allPlayerIds)
        {
            if (NetworkClient.spawned.TryGetValue(id, out var identity))
            {
                var p = identity.GetComponent<PlayerController>();
                if (p != null)
                    list.Add(p);
            }
        }

        RoomCanvasController.Instance.RefreshPlayerList(list);
    }

    void OnRoomStateChanged(RoomState oldState,RoomState newState)
    {
        if (!isClient) return;

        ApplyRoomUI(newState);
    }

    void ApplyRoomUI(RoomState state)
    {
        if (RoomCanvasController.Instance == null)
            return;

        List<PlayerController> uiPlayers = new List<PlayerController>();

        foreach (uint id in allPlayerIds)
        {
            if (NetworkClient.spawned.TryGetValue(id, out var identity))
            {
                var p = identity.GetComponent<PlayerController>();
                if (p != null)
                    uiPlayers.Add(p);
            }
        }

        switch (state)
        {
            case RoomState.Waiting:

                RoomCanvasController.Instance
                    .RefreshPlayerList(uiPlayers);

                RoomCanvasController.Instance
                    .ShowWaiting();
                break;


            case RoomState.Preparing:

                RoomCanvasController.Instance
                    .RefreshPlayerList(uiPlayers);

                RoomCanvasController.Instance
        .ShowPreparing();

                RoomCanvasController.Instance
                    .SetPrepareEnd(prepareEndTimestamp);
                break;


            case RoomState.Generating:

                RoomCanvasController.Instance
                    .RefreshPlayerList(uiPlayers);

                RoomCanvasController.Instance
                    .ShowGenerating();

                RoomCanvasController.Instance
                    .SetGenerateEnd(generateEndTimestamp);
                break;


            case RoomState.Playing:

                RoomCanvasController.Instance
                    .ShowPlaying();
                break;
        }
    }

    // 服务器启动
    public override void OnStartServer()
    {
        CacheSpawnPoints();
        ChangeState(RoomState.Waiting);
    }
    // 客户端连接
    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(WaitAndSync());
    }

    IEnumerator WaitAndSync()
    {
        // 等 SyncVar 完全同步
        yield return null;

        ApplyRoomUI(currentState);

        // 强制刷新时间
        RoomCanvasController.Instance?.SetPrepareEnd(prepareEndTimestamp);
        RoomCanvasController.Instance?.SetGenerateEnd(generateEndTimestamp);

        // 客户端启动后如果seed已同步，直接生成
        if (hasMapSeed)
            WallVisibilityController.Instance?.ApplySeed(currentMapSeed);
    }

    public void RegisterPlayer(PlayerController p)
    {
        uint id = p.netId;

        if (!isServer) return;

        if (!allPlayers.Contains(id))
            allPlayers.Add(id);
        // 第一个加入的是host
        if (allPlayers.Count == 1)
        {
            p.isHostPlayer = true;
        }


        if (!allPlayerIds.Contains(id))
            allPlayerIds.Add(id);


        TargetSyncState(p.connectionToClient);

        if (currentState == RoomState.Waiting)
            CheckCanStart();


        else if (currentState == RoomState.Playing || currentState == RoomState.Generating)
        {
            p.SetSpawnState(Vector3.zero, false);
            TargetShowSpectatorMsg(p.connectionToClient);
        }
    }

    [TargetRpc]
    void TargetSyncState(NetworkConnection conn)
    {
        ApplyRoomUI(currentState);
        RoomCanvasController.Instance?.SetPrepareEnd(prepareEndTimestamp);
        RoomCanvasController.Instance?.SetGenerateEnd(generateEndTimestamp);

        // 刚进房就补刷一次地图
        if (hasMapSeed)
            WallVisibilityController.Instance?.ApplySeed(currentMapSeed);
    }

    public void UnregisterPlayer(PlayerController p)
    {
        uint id = p.netId;

        if (!isServer) return;

        allPlayers.Remove(id);
        allPlayerIds.Remove(id);


        if (currentState == RoomState.Playing)
        {
            OnPlayerDead(p);
        }

        if (allPlayers.Count < 2)
        {
            if (matchFlowRoutine != null)
            {
                StopCoroutine(matchFlowRoutine);
                matchFlowRoutine = null;
            }

            preparingStarted = false;
            ChangeState(RoomState.Waiting);
        }
    }

    void CheckCanStart()
    {
        Debug.Log("当前人数: " + allPlayers.Count);

        if (allPlayers.Count >= 2 && !preparingStarted)
        {
            preparingStarted = true;
            matchFlowRoutine = StartCoroutine(PreparingRoutine());
        }
    }

    IEnumerator PreparingRoutine()
    {
        ChangeState(RoomState.Preparing);

        prepareEndTimestamp = NetworkTime.time + prepareTime;

        yield return new WaitUntil(() =>
            NetworkTime.time >= prepareEndTimestamp
        );

        matchPlayers = new List<uint>(allPlayerIds);

        StartCoroutine(GenerateRoutine());
    }

    IEnumerator GenerateRoutine()
    {
        ChangeState(RoomState.Generating);

        long seed = ((long)Random.Range(int.MinValue, int.MaxValue) << 32)
                    ^ (uint)Random.Range(int.MinValue, int.MaxValue);

        //  写入SyncVar（晚加入的人也能拿到）
        currentMapSeed = seed;
        hasMapSeed = true;

        // 保证sever服务器可以生成种子
        WallVisibilityController.Instance?.ApplySeed(seed);

        // 仍然可以保留RPC让“当前在线的人立刻生成”
        RpcGenerateMap(seed);

        generateEndTimestamp = NetworkTime.time + generateTime;

        yield return new WaitUntil(() => NetworkTime.time >= generateEndTimestamp);

        
        SpawnPlayers();
        ChangeState(RoomState.Playing);
        matchFlowRoutine = null;
        SpawnPickups();
    }

    void SpawnPickups()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return;

        // 5等概率：0=空(不刷)，1~4=刷4种道具
        int roll = Random.Range(0, 5);
        if (roll == 0)
            return; // 这局不刷道具

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // roll: 1~4 -> 映射到 PickupType: 0~3
        PickupType type = (PickupType)(roll - 1);

        GameObject obj = Instantiate(pickupPrefab, point.position, Quaternion.identity);

        PickupItem item = obj.GetComponent<PickupItem>();
        item.pickupType = type;

        NetworkServer.Spawn(obj);
    }

    void SpawnPlayers()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            CacheSpawnPoints();
        }

        foreach (uint id in matchPlayers)
        {
            if (!NetworkServer.spawned.TryGetValue(id, out NetworkIdentity identity))
                continue;

            PlayerController p = identity.GetComponent<PlayerController>();
            p.isHostPlayer = (id == matchPlayers[0]); // 第一个进房的人

            Vector3 spawnPos = GetRandomSpawn();

            // 1. 服务器重置状态
            p.Respawn(spawnPos);

            // 2. 强制同步客户端位置/状态
            p.TargetRespawn(p.connectionToClient, spawnPos);
        }
    }

    Vector3 GetRandomSpawn()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("没有设置出生点");
            return Vector3.zero;
        }

        int index = Random.Range(0, spawnPoints.Length);
        return spawnPoints[index].position;
    }

    [ClientRpc]
    void RpcGenerateMap(long seed)
    {

        // Host已经执行过
        if (isServer)
            return;

        Debug.Log(
            "根据随机种子生成地图: " +
            seed
        );

        // 所有端收到seed后，用同样算法点亮同样的墙
        WallVisibilityController.Instance?.ApplySeed(seed);
    }

    public void OnPlayerDead(PlayerController dead)
    {
        uint id = dead.netId;

        if (!allPlayerIds.Contains(id))
            return;

        if (currentState != RoomState.Playing)
            return;

        // 从临时比赛集合中判断
        List<uint> alive = new List<uint>();

        foreach (uint pid in allPlayerIds)
        {
            if (NetworkServer.spawned.TryGetValue(pid, out var identity))
            {
                var pc = identity.GetComponent<PlayerController>();
                if (pc != null && pc.isAlive)
                    alive.Add(pid);
            }
        }

        if (alive.Count == 1)
        {
            StartCoroutine(SettlementRoutine(
                NetworkServer.spawned[alive[0]].GetComponent<PlayerController>()
            ));
        }
        else if (alive.Count == 0)
        {
            StartCoroutine(SettlementRoutine(null));
        }
    }

    IEnumerator SettlementRoutine(PlayerController winner)
    {
        if (settling) yield break;
        settling = true;

        try
        {
            ChangeState(RoomState.Settlement);

            if (winner != null)
                RpcShowWin(winner.netId);
            else
                RpcDraw();

            yield return new WaitForSeconds(settleTime);

            preparingStarted = false;

            if (allPlayerIds.Count >= 2)
                CheckCanStart();
            else
                ChangeState(RoomState.Waiting);
        }
        finally
        {
            settling = false;
        }
    }

    void ChangeState(RoomState newState)
    {
        currentState = newState;

        Debug.Log(
            "房间状态 -> " +
            newState
        );

    }

    [TargetRpc]
    void TargetShowSpectatorMsg(NetworkConnection conn)
    {
        KickToastUI.Instance?.Show("游戏进行中，请等待本场比赛结束", UIContext.Game);
    }

    [ClientRpc]
    void RpcShowWin(uint winnerId)
    {
        string winnerName = "Unknown";

        foreach (var kv in NetworkClient.spawned)
        {
            PlayerController p =
                kv.Value.GetComponent<PlayerController>();

            if (p != null && p.netId == winnerId)
            {
                PlayerData data =
                    p.GetComponent<PlayerData>();

                if (data != null)
                    winnerName = data.playerName;

                break;
            }
        }

        RoomCanvasController.Instance?
            .ShowSettlement(winnerName);
    }

    [ClientRpc]
    void RpcDraw()
    {
        RoomCanvasController.Instance?.ShowSettlement("平局");
    }

    public void OnServerGameSceneReady()
    {
        CacheSpawnPoints();
    }

    // 服务器调用：触发所有端震动
    [Server]
    public void ServerShakeAll(float duration, float strength)
    {
        RpcShakeAll(duration, strength);
    }

    [ClientRpc]
    void RpcShakeAll(float duration, float strength)
    {
        CameraShakeManager.Instance?.Shake(duration, strength);
    }


    void CacheSpawnPoints()
    {
        GameObject[] points = GameObject.FindGameObjectsWithTag("SpawnPoint");

        spawnPoints = new Transform[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            spawnPoints[i] = points[i].transform;
        }

        Debug.Log("出生点加载完成: " + spawnPoints.Length);
    }
    // 退出时清理状态
    public override void OnStopServer()
    {
        StopAllCoroutines();

        matchPlayers.Clear();

        preparingStarted = false;
        settling = false;
        matchFlowRoutine = null;

    }
    // 状态复位
    public void FullReset()
    {
        StopAllCoroutines();

        matchPlayers.Clear();

        preparingStarted = false;
        settling = false;

        matchFlowRoutine = null;
        spawnPoints = null;

        currentState = RoomState.Waiting;

        prepareEndTimestamp = 0;
        generateEndTimestamp = 0;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}