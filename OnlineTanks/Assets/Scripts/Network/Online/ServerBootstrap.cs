using Mirror;
using System.Net;
using UnityEngine;

public class ServerBootstrap : MonoBehaviour
{
    public static ServerBootstrap Instance { get; private set; }
    int maxPlayers = 4;
    void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ServicePointManager.SecurityProtocol =
            SecurityProtocolType.Tls12;

        Debug.Log(SystemInfo.operatingSystem);
    }
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


    void Start()
    {
        bool isServerBuild = false;
        int port = 7777;
        int maxPlayers = 4;

        string[] args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-server")
            {
                isServerBuild = true;
            }

            if (args[i] == "-port" && i + 1 < args.Length)
            {
                int.TryParse(args[i + 1], out port);
            }

            if (args[i] == "-maxPlayers" && i + 1 < args.Length)
            {
                int.TryParse(args[i + 1], out maxPlayers);
            }
        }

        // 客户端直接退出，不执行服务端逻辑
        if (!isServerBuild)
        {
            Debug.Log("客户端模式，不启动Mirror服务器");
            return;
        }

        Debug.Log("Dedicated Server启动，端口：" + port);

        var manager = NetworkManager.singleton;
        manager.maxConnections = maxPlayers;

        if (manager == null)
        {
            Debug.LogError("没有找到NetworkManager");
            return;
        }

        var transport =
            manager.GetComponent<kcp2k.KcpTransport>();

        if (transport == null)
        {
            Debug.LogError("没有找到KcpTransport");
            return;
        }

        transport.Port = (ushort)port;

        manager.StartServer();

        Debug.Log("Mirror监听已开启：" + port);
    }

}