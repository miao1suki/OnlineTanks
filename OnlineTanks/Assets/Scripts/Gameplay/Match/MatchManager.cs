using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance;

    [SyncVar]
    double endTimestamp;

    List<PlayerController> GetAllClientPlayers()
    {
        List<PlayerController> result =
            new List<PlayerController>();

        foreach (var kv in NetworkClient.spawned)
        {
            PlayerController p =
                kv.Value.GetComponent<PlayerController>();

            if (p != null)
                result.Add(p);
        }

        return result;
    }

    [SyncVar(hook = nameof(OnRoomStateChanged))]
    public RoomState currentState;

    [SyncVar(hook = nameof(OnPrepareEndChanged))]
    double prepareEndTimestamp;

    [SyncVar(hook = nameof(OnGenerateEndChanged))]
    double generateEndTimestamp;

    public Transform[] spawnPoints;

    List<PlayerController> players =
        new List<PlayerController>();

    List<PlayerController> matchPlayers =
        new List<PlayerController>();

    List<PlayerController> spectators =
        new List<PlayerController>();

    [Header("准备阶段时长")]
    public float prepareTime = 10f;
    [Header("地图生成阶段时长")]
    public float generateTime = 2f;
    [Header("游戏结算时长")]
    public float settleTime = 5f;

    bool preparingStarted;
    bool settling;
    Coroutine matchFlowRoutine;


    void Awake()
    {
        Instance = this;
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

    // 刷新玩家列表
    void RefreshRoomPlayerUI()
    {
        RpcRefreshPlayerList();
    }

    [ClientRpc]
    void RpcRefreshPlayerList()
    {
        if (RoomCanvasController.Instance == null)
            return;

        if (currentState != RoomState.Waiting &&
            currentState != RoomState.Preparing &&
            currentState != RoomState.Generating)
            return;

        RoomCanvasController.Instance
            .RefreshPlayerList(
                GetAllClientPlayers()
            );
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

        List<PlayerController> uiPlayers =
            GetAllClientPlayers();

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
        ChangeState(RoomState.Waiting);
    }
    // 客户端连接
    public override void OnStartClient()
    {
        base.OnStartClient();

        ApplyRoomUI(currentState);
    }

    public void RegisterPlayer(PlayerController p)
    {
        players.Add(p);
        RefreshRoomPlayerUI();

        switch (currentState)
        {
            case RoomState.Waiting:
                CheckCanStart();
                break;

            case RoomState.Preparing:
                break;

            case RoomState.Playing:
                spectators.Add(p);

                p.SetSpawnState(Vector3.zero, false);
                TargetShowSpectatorMsg(
                    p.connectionToClient
                );
                break;
        }
    }

    public void UnregisterPlayer(PlayerController p)
    {
        players.Remove(p);
        RefreshRoomPlayerUI();

        if (matchPlayers.Contains(p))
        {
            matchPlayers.Remove(p);

            if (currentState == RoomState.Playing)
            {
                if (matchPlayers.Count == 1)
                {
                    StartCoroutine(
                        SettlementRoutine(
                            matchPlayers[0]
                        )
                    );

                    return;
                }

                if (matchPlayers.Count == 0)
                {
                    StartCoroutine(
                        SettlementRoutine(null)
                    );

                    return;
                }
            }
        }

        if (players.Count < 2)
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
        Debug.Log(
        "当前人数: " +
        players.Count
    );
        if (players.Count >= 2 && !preparingStarted)
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

        matchPlayers = new List<PlayerController>(players);

        StartCoroutine(GenerateRoutine());
    }

    IEnumerator GenerateRoutine()
    {
        ChangeState(RoomState.Generating);

        int seed =
            Random.Range(0, 999999);

        RpcGenerateMap(seed);

        generateEndTimestamp = NetworkTime.time + generateTime;

        yield return new WaitUntil(() =>
            NetworkTime.time >= generateEndTimestamp
        );

        SpawnPlayers();

        ChangeState(RoomState.Playing);
        matchFlowRoutine = null;
    }

    void SpawnPlayers()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            CacheSpawnPoints();
        }

        foreach (var p in matchPlayers)
        {
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
    void RpcGenerateMap(int seed)
    {
        Debug.Log(
            "根据随机种子生成地图: " +
            seed
        );
    }

    public void OnPlayerDead(PlayerController dead)
    {
        if (!matchPlayers.Contains(dead))
            return;

        matchPlayers.Remove(dead);

        if (matchPlayers.Count == 1)
        {
            StartCoroutine(
                SettlementRoutine(
                    matchPlayers[0]
                )
            );
        }

        else if (matchPlayers.Count == 0)
        {
            StartCoroutine(
                SettlementRoutine(
                    null
                )
            );
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

            if (players.Count >= 2)
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
        Debug.Log(
            "游戏进行中，请等待本场比赛结束"
        );
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

        players.Clear();
        matchPlayers.Clear();
        spectators.Clear();

        preparingStarted = false;
        settling = false;
        matchFlowRoutine = null;

    }
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}