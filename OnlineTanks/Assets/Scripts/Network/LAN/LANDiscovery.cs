using Mirror;
using Mirror.Discovery;
using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System;

[System.Serializable]
public class DiscoveryRequest : NetworkMessage { }

[System.Serializable]
public class DiscoveryResponse : NetworkMessage
{
    public long serverId;
    public string roomName; //房间名
    public string uri;
}

public class LANDiscovery : NetworkDiscoveryBase<DiscoveryRequest, DiscoveryResponse>
{

    public static LANDiscovery Instance { get; private set; }
    public string currentRoomName = "新建房间名"; // 当前房间名

    public Action<DiscoveryResponse, IPEndPoint> OnServerFoundCustom;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    protected override DiscoveryRequest GetRequest()
    {
        Debug.Log("客户端发送发现广播");
        return new DiscoveryRequest();
    }

    protected override DiscoveryResponse ProcessRequest(DiscoveryRequest request, IPEndPoint endpoint)
    {
        Debug.Log("Host收到发现请求");
        return new DiscoveryResponse
        {
            serverId = ServerId,
            roomName = currentRoomName,
            uri = transport.ServerUri().ToString()
        };
    }

    protected override void ProcessResponse(DiscoveryResponse response, IPEndPoint endpoint)
    {
        Debug.Log("客户端收到房间回应");
        Debug.Log($"发现服务器: {endpoint.Address}");

        OnServerFoundCustom?.Invoke(response, endpoint);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
