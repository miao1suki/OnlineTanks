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
        networkManager.StartServer();
        Debug.Log("局域网房间已创建，服务器启动");

        // 开始广播，让局域网其他客户端发现
        discovery.AdvertiseServer();
        Debug.Log("局域网房间广播已开启");
    }

    public void StopRoom()
    {
        if (!networkManager) networkManager = NetworkManager.singleton;
        if (!discovery) discovery = FindFirstObjectByType<LANDiscovery>();

        networkManager.StopServer();
        discovery.StopDiscovery();
        Debug.Log("房间已关闭");
    }
}