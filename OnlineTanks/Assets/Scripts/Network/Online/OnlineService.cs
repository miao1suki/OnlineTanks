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
        string url = "http://meowgame.cloud/api/rooms";

        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("获取房间失败: " + www.error);
            yield break;
        }

        string json = www.downloadHandler.text;

        // 假设返回数组
        RoomInfo[] rooms = JsonHelper.FromJson<RoomInfo>(json);

        foreach (var room in rooms)
        {
            OnRoomFound?.Invoke(room);
        }
    }

    public void Connect(RoomInfo room)
    {
        NetworkManager.singleton.networkAddress = room.address;
        NetworkManager.singleton.StartClient();
    }
}