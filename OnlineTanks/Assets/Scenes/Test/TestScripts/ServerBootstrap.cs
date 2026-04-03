using Mirror;
using UnityEngine;

public class ServerBootstrap : MonoBehaviour
{
    void Start()
    {
        string[] args = System.Environment.GetCommandLineArgs();

        foreach (string arg in args)
        {
            if (arg == "-server")
            {
                Debug.Log("服务器模式启动");
                NetworkManager.singleton.StartServer();
            }
        }
    }
}
