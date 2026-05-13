using UnityEngine;
using System;
using Mirror;
using Mirror.Examples.Common.Controllers.Player;
public class NetworkManagerCustom : NetworkManager
{
    public static NetworkManagerCustom Instance { get; private set; }

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


    public override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        base.Awake();
    }
    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // 注册：服务端拒绝原因
        NetworkClient.RegisterHandler<ServerRejectMessage>(OnServerReject, false);
    }

    void OnServerReject(ServerRejectMessage msg)
    {
        // Lobby 场景里显示
        KickToastUI.Instance?.Show(msg.reason, UIContext.Lobby);
    }

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
        Debug.LogWarning("[NET][ClientDisconnect] local client disconnected");

        base.OnClientDisconnect();

        // 你原来的逻辑
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
        uint netId = conn.identity != null ? conn.identity.netId : 0;
        Debug.LogWarning($"[NET][ServerDisconnect] connId={conn.connectionId} netId={netId} addr={conn.address}");

        if (conn.identity != null)
        {
            PlayerController player = conn.identity.GetComponent<PlayerController>();
            if (player != null)
            {
                // 先把 HitBox 一起销毁
                if (player.hitBox != null && player.hitBox.gameObject != null && player.hitBox.isServer)
                {
                    NetworkServer.Destroy(player.hitBox.gameObject);
                    player.hitBox = null;
                }

                MatchManager.Instance.UnregisterPlayer(player);
            }
        }

        base.OnServerDisconnect(conn);
    }

    //服务器生成玩家实例
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // numPlayers = 已经在房间里的玩家数量（已经AddPlayerForConnection的）
        if (numPlayers >= maxConnections)
        {
            // 发送原因给客户端
            conn.Send(new ServerRejectMessage { reason = "房间已满，无法加入" });

            // 再断开
            conn.Disconnect();
            return;
        }

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

        MatchManager.Instance?.FullReset();
        RoomCanvasController.Instance?.ResetUI();

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

    public struct ServerRejectMessage : NetworkMessage
    {
        public string reason;
    }

    // 获取断线信息调试

    public override void OnClientError(TransportError error, string reason)
    {
        Debug.LogError($"[NET][ClientError] error={error} reason={reason}");
        base.OnClientError(error, reason);
    }
}

