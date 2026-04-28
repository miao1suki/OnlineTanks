using Mirror;
using Mirror.Discovery;
using System;
using TMPro;
using UnityEngine;


public class LANRoomCreator : MonoBehaviour
{
    public NetworkManager networkManager;
    public LANDiscovery discovery;
    public TMP_InputField roomNameInput;


    public void CreateRoom()
    {
        if (!networkManager) networkManager = NetworkManager.singleton;
        if (!discovery) discovery = FindFirstObjectByType<LANDiscovery>();

        string roomName = roomNameInput.text;
        Debug.Log(roomName);
        discovery.currentRoomName = string.IsNullOrWhiteSpace(roomName) ? "新建房间名" : roomName;

        // 启动服务器
        networkManager.StartHost();
        Debug.Log("局域网房间已创建，服务器启动");

        // 开始广播，让局域网其他客户端发现
        discovery.AdvertiseServer();
        Debug.Log("局域网房间广播已开启");
    }

    public void StopRoom()
    {
        if (!networkManager) networkManager = NetworkManager.singleton;
        if (!discovery) discovery = FindFirstObjectByType<LANDiscovery>();

        // 先停广播/发现
        discovery?.StopDiscovery();

        // 再按实际模式停网络
        if (NetworkServer.active && NetworkClient.isConnected)
            networkManager.StopHost();
        else if (NetworkClient.isConnected)
            networkManager.StopClient();
        else if (NetworkServer.active)
            networkManager.StopServer();

        Debug.Log("房间已关闭");
    }
}