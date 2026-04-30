using Mirror;
using System;
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
    [Header("lanNameInput")]
    public TMP_InputField lanRoomNameInput; // LAN房间名称输入框
    [Header("serverNameInput")]
    public TMP_InputField serverRoomNameInput;// 服务器房间名称输入框
    [Header("PlayerNameInput")]
    public TMP_InputField playerNameInput;// 玩家名称输入框
    [Header("Server UI")]
    public GameObject serverRoomListParent;
    public Button serverSearchButton;


    public static int LocalColorIndex = 0;// 本地玩家颜色（默认绿色）
    public PlayerPreview preview;
    public static string LocalPlayerName = "Player";
    string lanRoomName = "新建房间名";
    string serverRoomName = "默认房间名";

    HashSet<long> discovered = new HashSet<long>(); //房间去重
    public enum UIMode
    {
        LAN,
        SERVER
    }

    public UIMode uiMode;

    private void Awake()
    {
        // 玩家名字输入
        if (playerNameInput != null)
        {
            playerNameInput.onValueChanged.AddListener(OnPlayerNameChanged);

            playerNameInput.SetTextWithoutNotify(
                LocalPlayerData.PlayerName
            );
        }

        // 房间名字输入
        if (lanRoomNameInput != null)
        {
            lanRoomNameInput.onValueChanged.AddListener(
                OnLanRoomNameChanged
            );
        }

        if (serverRoomNameInput != null)
        {
            serverRoomNameInput.onValueChanged.AddListener(
                OnServerRoomNameChanged
            );
        }


        // LAN事件
        if (RoomService.Instance != null)
        {
            RoomService.Instance.OnRoomFound += OnLANRoomFound;
        }


        if (lanSearchButton != null)
            lanSearchButton.onClick.AddListener(OnClickSearch);

        if (serverSearchButton != null)
            serverSearchButton.onClick.AddListener(OnClickSearch);

    }
    private void OnDestroy()
    {
        // 防止事件泄漏
        if (RoomService.Instance != null)
            RoomService.Instance.OnRoomFound -= OnLANRoomFound;
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
        LANDiscovery.Instance.StopDiscovery();
        LANDiscovery.Instance.StartDiscovery();
        RoomService.Instance.StartSearch();
    }

    // 收到服务器回应
    async void HandleRoom(string roomName, IPEndPoint endpoint, long serverId,
                int playerCount = 0, int maxPlayers = 0, int ping = -1)
    {
        if (discovered.Contains(serverId)) return;
        discovered.Add(serverId);

        GameObject parent = (uiMode == UIMode.LAN) ? lanRoomListParent : serverRoomListParent;

        GameObject itemGO = Instantiate(roomItemPrefab, parent.transform, false);
        RoomItem item = itemGO.GetComponent<RoomItem>();

        item.roomNameText.text = roomName;

        // 人数显示
        item.coount.text = (maxPlayers > 0) ? $"{playerCount}/{maxPlayers}" : $"{playerCount}/--";

        // 延迟显示
        // 先显示占位
        item.delay.text = "检测中...";

        // 记住这次创建的GO引用（用于await回来后判断是否已被销毁）
        GameObject itemRef = itemGO;

        // 异步Ping
        int realPing = await PingUtility.GetPing(endpoint.Address);

        // await回来后，有可能列表被清了，itemGO 已经没了
        if (itemRef == null || item == null)
            return;

        item.delay.text = realPing >= 0 ? $"{realPing} ms" : "--";


        // 只限制 Server 房间，LAN 不限制
        bool full = (uiMode == UIMode.SERVER && maxPlayers > 0 && playerCount >= maxPlayers);

        // 按钮置灰
        item.joinButton.interactable = !full;

        item.joinButton.onClick.AddListener(() =>
        {
            if (full)
            {
                KickToastUI.Instance?.Show("房间已满，无法加入", UIContext.Lobby);
                return;
            }

            if (uiMode == UIMode.LAN)
            {
                //  LAN：只用 address
                RoomService.Instance?.Connect(endpoint.Address.ToString());
            }
            else
            {
                //  Server：必须用 endpoint
                RoomService.Instance?.Connect(endpoint);
            }
        });
    }
    void OnLANRoomFound(DiscoveryResponse info, IPEndPoint endpoint)
    {
        HandleRoom(
            info.roomName,
            endpoint,
            info.serverId,
            info.playerCount,
            info.maxPlayers,
            EstimateLanPing(endpoint)
        );
    }

    int EstimateLanPing(IPEndPoint ep)
    {
        return UnityEngine.Random.Range(3, 15);
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



    void OnPlayerNameChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        LocalPlayerData.PlayerName = value;

        Debug.Log(
            "本地保存玩家名字: " + value
        );
    }


    void OnLanRoomNameChanged(string value)
    {
        lanRoomName = value;

        Debug.Log(
            "LAN房间名修改: " + value
        );
    }


    void OnServerRoomNameChanged(string value)
    {
        serverRoomName = value;

        Debug.Log(
            "服务器房间名修改: " + value
        );
    }


    public void OnClickSelectColor(int index)
    {
        LocalPlayerData.ColorIndex = index;

        Debug.Log("选择颜色ID: " + index);
        // 更新预览
        preview?.ApplyColor(index);
    }
   
}
