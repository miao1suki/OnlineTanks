using UnityEngine;
using System;
using Mirror;
public class NetworkManagerCustom : NetworkManager
{
    public static Action<bool> OnConnectionStatusChanged;

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("连接成功");
        OnConnectionStatusChanged?.Invoke(true);
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("连接断开");
        OnConnectionStatusChanged?.Invoke(false);
    }
}
