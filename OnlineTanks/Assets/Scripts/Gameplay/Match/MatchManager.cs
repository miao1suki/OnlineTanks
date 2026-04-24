using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance;

    [SyncVar]
    public RoomState currentState;

    public Transform[] spawnPoints;

    List<PlayerController> players =
        new List<PlayerController>();

    List<PlayerController> matchPlayers =
        new List<PlayerController>();

    List<PlayerController> spectators =
        new List<PlayerController>();


    public float prepareTime = 10f;
    public float settleTime = 5f;

    bool preparingStarted;
    bool settling;


    void Awake()
    {
        Instance = this;
    }


    public override void OnStartServer()
    {
        ChangeState(RoomState.Waiting);
    }

    public void RegisterPlayer(PlayerController p)
    {
        players.Add(p);

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
            StopAllCoroutines();

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
            StartCoroutine(PreparingRoutine());
        }
    }

    IEnumerator PreparingRoutine()
    {
        ChangeState(
            RoomState.Preparing
        );

        yield return new WaitForSeconds(
            prepareTime
        );

        matchPlayers =
            new List<PlayerController>(
                players
            );

        StartCoroutine(
            GenerateRoutine()
        );
    }

    IEnumerator GenerateRoutine()
    {
        ChangeState(
            RoomState.Generating
        );

        int seed =
            Random.Range(
                0,
                999999
            );

        RpcGenerateMap(seed);

        yield return new WaitForSeconds(2f);

        SpawnPlayers();

        ChangeState(
            RoomState.Playing
        );
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
        Debug.Log(
            "胜利玩家:" + winnerId
        );
    }

    [ClientRpc]
    void RpcDraw()
    {
        Debug.Log("平局");
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
}