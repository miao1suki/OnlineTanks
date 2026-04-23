using Mirror;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameObject roomItemPrefab; // 房间UI预制体
    [Header("LAN UI")]
    public GameObject lanRoomListParent; // 列表父物体
    public Button lanSearchButton;            // 搜索按钮
    public TMP_InputField lanNameInput; // 名字输入框
    [Header("Server UI")]
    public GameObject serverRoomListParent;
    public Button serverSearchButton;
    public TMP_InputField serverNameInput;

    public static int LocalColorIndex = 0;// 本地玩家颜色（默认绿色）
    public PlayerPreview preview;
    public static string LocalPlayerName = "Player";

    HashSet<string> discovered = new HashSet<string>(); //房间去重
    public enum UIMode
    {
        LAN,
        SERVER
    }

    public UIMode uiMode;

    private void Awake()
    {
        // LAN事件
        if (RoomService.Instance != null)
        {
            RoomService.Instance.OnRoomFound += OnLANRoomFound;
        }

        // Server事件
        if (OnlineService.Instance != null)
        {
            OnlineService.Instance.OnRoomFound += OnServerRoomFound;
        }

        if (lanSearchButton != null)
            lanSearchButton.onClick.AddListener(OnClickSearch);

        if (serverSearchButton != null)
            serverSearchButton.onClick.AddListener(OnClickSearch);

        if (lanNameInput != null)
            lanNameInput.onValueChanged.AddListener(OnNameChanged);

        if (serverNameInput != null)
            serverNameInput.onValueChanged.AddListener(OnNameChanged);
    }
    private void OnDestroy()
    {
        // 防止事件泄漏
        if (RoomService.Instance != null)
            RoomService.Instance.OnRoomFound -= OnLANRoomFound;

        if (OnlineService.Instance != null)
            OnlineService.Instance.OnRoomFound -= OnServerRoomFound;
    }

    public void OnClickLAN()
    {
        uiMode = UIMode.LAN;
        RoomService.Instance.SetModeLAN();
    }
    public void OnClickServer()
    {
        uiMode = UIMode.SERVER;
        RoomService.Instance.SetModeServer();
    }

    // 点击搜索按钮
    public void OnClickSearch()
    {
        Debug.Log("开始搜索房间");

        ClearRoomList();

        // 开始搜索
        RoomService.Instance.StartSearch();
    }

    // 收到服务器回应
    void HandleRoom(string roomName, string address)
    {
        if (discovered.Contains(address)) return;
        discovered.Add(address);

        GameObject parent = (uiMode == UIMode.LAN)
            ? lanRoomListParent
            : serverRoomListParent;

        GameObject itemGO = Instantiate(roomItemPrefab, parent.transform, false);
        RoomItem item = itemGO.GetComponent<RoomItem>();

        item.roomNameText.text = roomName;

        item.joinButton.onClick.AddListener(() =>
        {
            ConnectToServer(address);
        });
    }
    void OnLANRoomFound(DiscoveryResponse info, IPEndPoint endpoint)
    {
        HandleRoom(info != null ? info.roomName : endpoint.Address.ToString(),
               endpoint.Address.ToString());
    }
    void OnServerRoomFound(RoomInfo room)
    {
        HandleRoom(room.roomName, room.address);
    }


    void ClearRoomList()
    {
        GameObject parent = (uiMode == UIMode.LAN)
        ? lanRoomListParent
        : serverRoomListParent;

        foreach (Transform child in parent.transform)
            Destroy(child.gameObject);

        discovered.Clear(); //清空去重列表
    }

    void ConnectToServer(string address)
    {
        Debug.Log("连接服务器: " + address);

        RoomService.Instance?.Connect(address);
    }

    void OnNameChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        LocalPlayerData.PlayerName = value;

        Debug.Log("本地保存名字: " + value);

    }
 

    public void OnClickSelectColor(int index)
    {
        LocalPlayerData.ColorIndex = index;

        Debug.Log("选择颜色ID: " + index);
        // 更新预览
        preview?.ApplyColor(index);
    }
   
}
