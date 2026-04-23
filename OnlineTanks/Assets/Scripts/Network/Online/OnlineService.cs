using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Mirror;

public class OnlineService : MonoBehaviour
{
    public static OnlineService Instance;

    public System.Action<RoomInfo> OnRoomFound;

    private void Awake()
    {
        Instance = this;
    }

    public void GetRoomList()
    {
        StartCoroutine(RequestRoomList());
    }

    IEnumerator RequestRoomList()
    {
        string url = "https://62.234.93.20/api/rooms";

        UnityWebRequest www = UnityWebRequest.Get(url);
        www.certificateHandler = new IgnoreSSL();
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("获取房间失败: " + www.error);
            yield break;
        }

        string json = www.downloadHandler.text;
        Debug.Log("房间数据: " + json);

        RoomListResponse data = JsonUtility.FromJson<RoomListResponse>(json);

        if (data == null || data.rooms == null)
        {
            Debug.LogError("房间解析失败");
            yield break;
        }

        foreach (var room in data.rooms)
        {
            OnRoomFound?.Invoke(room);
        }
    }

    public void Connect(RoomInfo room)
    {
        var transport =
        NetworkManager.singleton.GetComponent<kcp2k.KcpTransport>();

        transport.Port = (ushort)room.port;
        NetworkManager.singleton.networkAddress = room.address;

        NetworkManager.singleton.StartClient();
    }
}
[System.Serializable]
public class RoomListResponse
{
    public RoomInfo[] rooms;
}