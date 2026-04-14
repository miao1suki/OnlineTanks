using Mirror;
using System.Net;
using UnityEngine;

public class RoomService : MonoBehaviour
{
    public static RoomService Instance;

    public enum RoomMode
    {
        LAN,
        Server
    }

    [Header("当前模式")]
    public RoomMode mode = RoomMode.LAN;

    private LANDiscovery discovery;

    public System.Action<DiscoveryResponse, IPEndPoint> OnRoomFound;


    private void Awake()
    {
        Instance = this;
        discovery = FindFirstObjectByType<LANDiscovery>();

        if (discovery != null)
        {
            discovery.OnServerFoundCustom += (info, endpoint) =>
            {
                if (mode != RoomMode.LAN) return;

                OnRoomFound?.Invoke(info, endpoint);
            };
        }
    }
    public void SetModeLAN()
    {
        mode = RoomMode.LAN;
        Debug.Log("已切换到 LAN 模式");
    }

    public void SetModeServer()
    {
        mode = RoomMode.Server;
        Debug.Log("已切换到 Server 模式（HTTP房间）");
    }

    // ========================
    // LAN模式：搜索房间
    // ========================
    public void StartSearchLAN()
    {
        discovery?.StartDiscovery();
    }

    // ========================
    // Server模式：预留接口（后面HTTP用）
    // ========================
    public void StartSearchServer()
    {
        if (mode != RoomMode.Server)
        {
            Debug.LogWarning("当前不是Server模式");
            return;
        }

        Debug.Log("Server模式：准备请求HTTP房间列表");
        // TODO: 后面接 OnlineService
    }

    public void Connect(string address)
    {
        NetworkManager.singleton.networkAddress = address;
        NetworkManager.singleton.StartClient();
    }


}