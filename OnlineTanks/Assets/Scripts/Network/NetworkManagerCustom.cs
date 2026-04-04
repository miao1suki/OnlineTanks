using UnityEngine;
using System;
using Mirror;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;
public class NetworkManagerCustom : NetworkManager
{
    // 连接类型枚举
    public enum ConnectionType
    {
        ServerStart,   // 自己开房间的服务端
        ClientConnect, // 客户端连接成功
        Disconnect     // 断开连接
    }

    // 事件：参数标识连接类型
    public static Action<ConnectionType> OnConnectionStatusChanged;

    // 客户端成功连接
    public override void OnClientConnect()
    {
        base.OnClientConnect();

        if (mode == NetworkManagerMode.Host) return;

            Debug.Log("客户端连接成功");
        OnConnectionStatusChanged?.Invoke(ConnectionType.ClientConnect);
    }

    // 客户端断开连接
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        if (mode == NetworkManagerMode.Host) return;

        Debug.Log("客户端断开连接");
        OnConnectionStatusChanged?.Invoke(ConnectionType.Disconnect);
    }

    // 服务端启动（房间创建）
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("服务端启动，房间已创建");
        OnConnectionStatusChanged?.Invoke(ConnectionType.ServerStart);
    }

    // 服务端停止
    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("服务端停止，房间已关闭");
        OnConnectionStatusChanged?.Invoke(ConnectionType.Disconnect);
    }
    
    //服务器生成玩家实例
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        GameObject player = Instantiate(playerPrefab);

        PlayerData data = player.GetComponent<PlayerData>();

        // 设置位置
        player.transform.position = Vector3.zero;
        NetworkServer.AddPlayerForConnection(conn, player);

        //编辑器显示ID
        player.name = "Player_" + player.GetComponent<NetworkIdentity>().netId;
    }
}

