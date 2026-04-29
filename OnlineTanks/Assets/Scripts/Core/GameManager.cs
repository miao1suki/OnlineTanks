using Mirror;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject[] DontDestroy;
    public GameObject LobbyCanvas;
    public static GameManager instance;

    bool leavingBySelf = false;

    bool cleaning = false;
    private void Awake()
    {
        // 单例防重复
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void OnEnable()
    {
        NetworkManagerCustom.OnConnectionStatusChanged += HandleConnectionStatus;
    }
    private void OnDisable()
    {
        NetworkManagerCustom.OnConnectionStatusChanged -= HandleConnectionStatus;
    }
    void Start()
    {
        foreach (GameObject obj in DontDestroy)
        {
            if (obj == null) continue;

            DontDestroyOnLoad(obj);
        }
    }

    void Update()
    {
        
    }

    private void HandleConnectionStatus(NetworkManagerCustom.ConnectionType type)
    {
        switch (type)
        {
            case NetworkManagerCustom.ConnectionType.ServerRunning:
                OnServerStarted();
                break;

            case NetworkManagerCustom.ConnectionType.Connected:
                Debug.Log("已连接服务器（进入大厅/等待进入房间）");
                break;

            case NetworkManagerCustom.ConnectionType.InRoom:
                OnClientConnected();
                break;

            case NetworkManagerCustom.ConnectionType.Disconnected:
                OnDisconnected();
                break;
        }
    }

    private void OnServerStarted()
    {
        Debug.Log("服务端房间启动逻辑");
        LobbyCanvas?.SetActive(false);
        // 在这里初始化房间状态或玩家列表
    }

    private void OnClientConnected()
    {
        Debug.Log("客户端加入房间逻辑");
        LobbyCanvas?.SetActive(false);
    }
    public void LeaveRoom()
    {
        bool isHost = NetworkServer.active && NetworkClient.active;

        if (isHost && NetworkClient.isConnected)
        {
            Debug.Log("LAN Host退出 -> Application.Quit()");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            return;
        }

        leavingBySelf = true;

        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();

        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        else if (NetworkServer.active)
            NetworkManager.singleton.StopServer();
    }

    private void OnDisconnected()
    {
        if (cleaning) return;
        cleaning = true;

        CleanupNetworkState();

        // 只有非主动离开才尝试补偿StopRoom
        if (!leavingBySelf)
        {
            LANRoomCreator creator = FindFirstObjectByType<LANRoomCreator>();
            if (creator != null)
                creator.StopRoom();
        }

        leavingBySelf = false;

        LobbyCanvas?.SetActive(true);

        cleaning = false;
    }


    public void CleanupNetworkState()
    {
        Debug.Log("开始清理联机状态");

        MatchManager.Instance?.FullReset();

        RoomCanvasController.Instance?.ResetUI();

        StopAllCoroutines();
    }


    public void ExitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();
    }

}
