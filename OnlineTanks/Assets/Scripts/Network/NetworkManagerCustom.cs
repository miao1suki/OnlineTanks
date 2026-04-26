using UnityEngine;
using System;
using Mirror;
using Mirror.Examples.Common.Controllers.Player;
public class NetworkManagerCustom : NetworkManager
{
    // 连接类型枚举
    public enum ConnectionType
    {
        Idle,
        Connecting,
        Connected,      
        InRoom,         
        ServerRunning,
        Disconnected
    }

    // 事件：参数标识连接类型
    public static Action<ConnectionType> OnConnectionStatusChanged;
    public static Action<string> OnJoinedRoom;

    public GameObject hitBoxPrefab;

    // 客户端成功连接
    public override void OnClientConnect()
    {
        base.OnClientConnect();

        if (NetworkServer.active)
        {
            Debug.Log("Host模式忽略ClientConnect");
            return;
        }

        NetworkClient.Ready();

        Debug.Log("网络连接成功（未进入房间）");
        OnConnectionStatusChanged?.Invoke(ConnectionType.Connected);
    }

    // 客户端断开连接
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        if (NetworkServer.active && NetworkClient.active)
            return;

        Debug.Log("客户端断开连接");
        OnConnectionStatusChanged?.Invoke(ConnectionType.Disconnected);
    }

    // 服务端启动（房间创建）
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("服务端启动，房间已创建");
        OnConnectionStatusChanged?.Invoke(ConnectionType.ServerRunning);
    }


    // 服务端停止
    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("服务端停止，房间已关闭");
        OnConnectionStatusChanged?.Invoke(ConnectionType.Disconnected);
    }
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            PlayerController player =
                conn.identity.GetComponent<PlayerController>();

            if (player != null)
            {
                MatchManager.Instance.UnregisterPlayer(player);
            }
        }

        base.OnServerDisconnect(conn);
    }

    //服务器生成玩家实例
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        GameObject player = Instantiate(playerPrefab);

        player.transform.position = Vector3.zero;

        NetworkServer.AddPlayerForConnection(conn, player);

        player.name =
            "Player_" +
            player.GetComponent<NetworkIdentity>().netId;

        GameObject box = Instantiate(hitBoxPrefab);
        PlayerController playerController = player.GetComponent<PlayerController>();

        NetworkServer.Spawn(box);

        PlayerHitBox hitBox = box.GetComponent<PlayerHitBox>();

        // 绑定玩家
        hitBox.Bind(playerController);

        // 让 player 持有引用
        playerController.hitBox = hitBox;

        MatchManager.Instance.RegisterPlayer(
            player.GetComponent<PlayerController>()
        );
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();

        Debug.Log("真正进入游戏场景");

        OnConnectionStatusChanged?.Invoke(ConnectionType.InRoom);

        OnJoinedRoom?.Invoke(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        if (sceneName == "Game")
        {
            Debug.Log("服务器进入Game场景");

            MatchManager.Instance?.OnServerGameSceneReady();
        }
    }
}

