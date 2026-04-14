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
    public string currentRoomName = "新建房间名"; // 当前房间名

    public Action<DiscoveryResponse, IPEndPoint> OnServerFoundCustom;

    protected override DiscoveryRequest GetRequest()
    {
        Debug.Log($"服务器房间名: {currentRoomName}");
        return new DiscoveryRequest();
    }

    protected override DiscoveryResponse ProcessRequest(DiscoveryRequest request, IPEndPoint endpoint)
    {
        return new DiscoveryResponse
        {
            serverId = ServerId,
            roomName = currentRoomName,
            uri = transport.ServerUri().ToString()
        };
    }

    protected override void ProcessResponse(DiscoveryResponse response, IPEndPoint endpoint)
    {
        Debug.Log($"发现服务器: {endpoint.Address}");

        OnServerFoundCustom?.Invoke(response, endpoint);
    }
}
