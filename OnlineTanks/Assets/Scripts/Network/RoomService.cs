using kcp2k;
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

            var resp = new DiscoveryResponse
            {
                roomName = room.roomName,
                playerCount = room.playerCount,
                maxPlayers = room.maxPlayers,
                serverId = room.address.GetHashCode() ^ room.port
            };
            OnRoomFound?.Invoke(resp, endpoint);
        };

        OnlineService.Instance.GetRoomList();
    }

    public void Connect(string address)
    {
        NetworkManager.singleton.networkAddress = address;

        Debug.Log($"连接服务器(LAN-默认端口): {address} (port 使用本机KCP设置)");
        NetworkManager.singleton.StartClient();
    }

    public void Connect(IPEndPoint endpoint)
    {
        // address
        NetworkManager.singleton.networkAddress = endpoint.Address.ToString();

        // port
        var transport = NetworkManager.singleton.GetComponent<KcpTransport>();
        if (transport == null)
        {
            Debug.LogError("KcpTransport 未挂载，无法设置端口");
            return;
        }
        transport.port = (ushort)endpoint.Port;

        Debug.Log($"连接服务器: {endpoint.Address}:{endpoint.Port}");
        NetworkManager.singleton.StartClient();
    }
}