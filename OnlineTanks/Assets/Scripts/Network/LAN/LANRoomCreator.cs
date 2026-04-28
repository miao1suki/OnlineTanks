using Mirror;
using Mirror.Discovery;
using System;
using TMPro;
using UnityEngine;


public class LANRoomCreator : MonoBehaviour
{
    public static LANRoomCreator Instance { get; private set; }

    public NetworkManager networkManager;
    public LANDiscovery discovery;
    public TMP_InputField roomNameInput;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void CreateRoom()
    {
        if (!networkManager) networkManager = NetworkManager.singleton;
        if (!discovery) discovery = FindFirstObjectByType<LANDiscovery>();
        GameManager.instance?.SetLanHost(true);



        string roomName = roomNameInput.text;
        Debug.Log(roomName);
        discovery.currentRoomName = string.IsNullOrWhiteSpace(roomName) ? "新建房间名" : roomName;

        discovery.StopDiscovery();
        // 启动服务器
        networkManager.StartHost();
        Debug.Log("局域网房间已创建，服务器启动");

        discovery.StartDiscovery(); // 重建socket
        // 开始广播，让局域网其他客户端发现
        discovery.AdvertiseServer();
        Debug.Log("局域网房间广播已开启");
    }

    public void StopRoom()
    {
        if (!networkManager) networkManager = NetworkManager.singleton;
        if (!discovery) discovery = FindFirstObjectByType<LANDiscovery>();


        // 再按实际模式停网络
        if (NetworkServer.active && NetworkClient.isConnected)
            networkManager.StopHost();
        else if (NetworkClient.isConnected)
            networkManager.StopClient();
        else if (NetworkServer.active)
            networkManager.StopServer();

        // 停广播/发现
        discovery?.StopDiscovery();


        GameManager.instance?.SetLanHost(false);
        Debug.Log("房间已关闭");
    }
}