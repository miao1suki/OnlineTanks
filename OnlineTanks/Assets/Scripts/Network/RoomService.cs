using Mirror;
using System.Net;
using UnityEngine;

public class RoomService : MonoBehaviour
{
    public static RoomService Instance;

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
                OnRoomFound?.Invoke(info, endpoint);
            };
        }
    }

    public void StartSearch()
    {
        discovery?.StartDiscovery();
    }

    public void Connect(string address)
    {
        NetworkManager.singleton.networkAddress = address;
        NetworkManager.singleton.StartClient();
    }
}