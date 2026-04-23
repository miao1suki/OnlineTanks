using Mirror;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject[] DontDestroy;
    public GameObject LobbyCanvas;
    public static GameManager instance;
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

    private void OnDisconnected()
    {
        Debug.Log("断开连接逻辑");
        LobbyCanvas?.SetActive(true);
    }


    public void ExitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();
    }

}
