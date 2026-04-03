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
    public Uri uri;
}

public class LANDiscovery : NetworkDiscoveryBase<DiscoveryRequest, DiscoveryResponse>
{
    public Action<DiscoveryResponse, IPEndPoint> OnServerFoundCustom;

    protected override DiscoveryRequest GetRequest()
    {
        return new DiscoveryRequest();
    }

    protected override DiscoveryResponse ProcessRequest(DiscoveryRequest request, IPEndPoint endpoint)
    {
        return new DiscoveryResponse
        {
            serverId = ServerId,
            uri = transport.ServerUri()
        };
    }

    protected override void ProcessResponse(DiscoveryResponse response, IPEndPoint endpoint)
    {
        Debug.Log($"发现服务器: {endpoint.Address}");

        OnServerFoundCustom?.Invoke(response, endpoint);
    }
}
