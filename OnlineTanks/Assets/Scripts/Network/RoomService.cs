using Mirror;
using System.Net;
using UnityEngine;

public class RoomService : MonoBehaviour
{
    public static RoomService Instance;

    public enum RoomMode { LAN, Server }
    public RoomMode mode = RoomMode.LAN;

    private LANDiscovery discovery;
    public System.Action<DiscoveryResponse, IPEndPoint> OnRoomFound;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        discovery = FindFirstObjectByType<LANDiscovery>();
        if (discovery != null)
            discovery.OnServerFoundCustom += HandleLanServerFound;
    }

    void OnDestroy()
    {
        if (discovery != null)
            discovery.OnServerFoundCustom -= HandleLanServerFound;

        if (Instance == this)
            Instance = null;
    }

    void HandleLanServerFound(DiscoveryResponse info, IPEndPoint endpoint)
    {
        if (mode != RoomMode.LAN) return;
        OnRoomFound?.Invoke(info, endpoint);
    }

    public void SetModeLAN() { mode = RoomMode.LAN; Debug.Log("已切换到 LAN 模式"); }
    public void SetModeServer() { mode = RoomMode.Server; Debug.Log("已切换到 Server 模式（HTTP房间）"); }

    public void StartSearch()
    {
        if (mode == RoomMode.LAN) StartSearchLAN();
        else StartSearchServer();
    }

    public void StartSearchLAN()
    {
        // 每次搜索前先 StopDiscovery 再 Start，避免残留状态
        discovery?.StopDiscovery();
        discovery?.StartDiscovery();
    }

    public void StartSearchServer()
    {
        if (mode != RoomMode.Server)
        {
            Debug.LogWarning("当前不是Server模式");
            return;
        }


        OnlineService.Instance.OnRoomFound = null;
        OnlineService.Instance.OnRoomFound += (room) =>
        {
            Debug.Log("HTTP房间: " + room.roomName);

            var ip = System.Net.Dns.GetHostAddresses(room.address)[0];
            var endpoint = new IPEndPoint(ip, room.port);

            var resp = new DiscoveryResponse { roomName = room.roomName };
            OnRoomFound?.Invoke(resp, endpoint);
        };

        OnlineService.Instance.GetRoomList();
    }

    public void Connect(string address)
    {
        NetworkManager.singleton.networkAddress = address;
        NetworkManager.singleton.StartClient();
    }
}