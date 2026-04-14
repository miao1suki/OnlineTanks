using Mirror;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameObject roomItemPrefab; // 房间UI预制体
    public GameObject roomListParent; // 列表父物体
    public Button searchButton;            // 搜索按钮
    public TMP_InputField nameInput; // 名字输入框
    public static int LocalColorIndex = 0;// 本地玩家颜色（默认绿色）
    public PlayerPreview preview;
    public static string LocalPlayerName = "Player";

    HashSet<string> discovered = new HashSet<string>(); //房间去重

    private void Awake()
    {
        //监听 RoomService
        if (RoomService.Instance != null)
        {
            RoomService.Instance.OnRoomFound += OnServerFound;
        }

        if (searchButton != null)
                searchButton.onClick.AddListener(OnClickSearch);

        if (nameInput != null)
        {
            nameInput.onValueChanged.AddListener(OnNameChanged); // 监听输入
            Debug.Log("已监听输入");
        }

    }
    private void OnDestroy()
    {
        // 防止事件泄漏
        if (RoomService.Instance != null)
        {
            RoomService.Instance.OnRoomFound -= OnServerFound;
        }
    }

    // 点击搜索按钮
    public void OnClickSearch()
    {
        Debug.Log("开始搜索局域网房间");

        ClearRoomList();

        RoomService.Instance?.StartSearch(); // 开始搜索
    }

    // 收到服务器回应
    void OnServerFound(DiscoveryResponse info, IPEndPoint endpoint)
    {
        string address = endpoint.Address.ToString();

        if (discovered.Contains(address)) return;
        discovered.Add(address);

        GameObject itemGO = Instantiate(roomItemPrefab, roomListParent.transform ,false);
        RoomItem item = itemGO.GetComponent<RoomItem>();

        item.roomNameText.text = info.roomName;  //在房间文本显示房间名


        item.joinButton.onClick.AddListener(() =>
        {
            ConnectToServer(address);
        });
    }

    void ClearRoomList()
    {
        foreach (Transform child in roomListParent.transform)
        {
            Destroy(child.gameObject);
        }

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
