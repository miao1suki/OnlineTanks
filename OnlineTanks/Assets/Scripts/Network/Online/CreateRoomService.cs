using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Mirror;
using TMPro;

public class CreateRoomService : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField roomNameInput;
    public TMP_Dropdown maxPlayerDropdown;

    public static CreateRoomService Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void OnClickCreateRoom()
    {
        string roomName = roomNameInput.text;

        if (string.IsNullOrWhiteSpace(roomName))
            roomName = "默认房间名";

        int maxPlayers = GetMaxPlayersFromDropdown();

        CreateRoom(roomName, maxPlayers);
    }

    int GetMaxPlayersFromDropdown()
    {
        // Dropdown 选项：0=2人 1=3人 2=4人
        switch (maxPlayerDropdown.value)
        {
            case 0: return 2;
            case 1: return 3;
            case 2: return 4;
        }
        return 4;
    }

    public void CreateRoom(string roomName, int maxPlayers)
    {
        StartCoroutine(CreateRoomCoroutine(roomName, maxPlayers));
    }

    IEnumerator CreateRoomCoroutine(string roomName, int maxPlayers)
    {
        string url = "https://meowgame.cloud/api/createRoom";

        WWWForm form = new WWWForm();
        form.AddField("name", roomName);
        form.AddField("maxPlayers", maxPlayers);

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        www.certificateHandler = new IgnoreSSL();
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("创建房间失败: " + www.error);
            yield break;
        }

        string json = www.downloadHandler.text;
        Debug.Log("创建成功: " + json);

        CreateRoomResponse res = JsonUtility.FromJson<CreateRoomResponse>(json);

        if (res.success)
        {
            ConnectToRoom(res.room);
        }
    }

    void ConnectToRoom(RoomInfo room)
    {
        Debug.Log("连接新房间: " + room.address + ":" + room.port);

        NetworkManager.singleton.networkAddress = room.address;
        NetworkManager.singleton.GetComponent<TelepathyTransport>().port = (ushort)room.port;

        NetworkManager.singleton.StartClient();
    }
}

[System.Serializable]
public class CreateRoomResponse
{
    public bool success;
    public RoomInfo room;
}