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
 
    private LANDiscovery discovery;
    HashSet<string> discovered = new HashSet<string>(); //房间去重

    private void Awake()
    {       
            discovery = FindFirstObjectByType<LANDiscovery>();

            if (discovery != null)
            {
                discovery.OnServerFoundCustom += OnServerFound; // 订阅回调
            }

            if (searchButton != null)
                searchButton.onClick.AddListener(OnClickSearch);
        
    }

    // 点击搜索按钮
    public void OnClickSearch()
    {
        Debug.Log("开始搜索局域网房间");

        ClearRoomList();

        discovery?.StartDiscovery(); // 开始搜索
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

        Debug.Log("需撰写房间名：" +info.roomName + "写后房间名：" + item.roomNameText.text);

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

        NetworkManager.singleton.networkAddress = address;
        NetworkManager.singleton.StartClient();
    }
}
