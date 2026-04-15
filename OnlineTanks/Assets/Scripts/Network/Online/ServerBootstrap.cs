using Mirror;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;

public class ServerBootstrap : MonoBehaviour
{
    private void Awake()
    {
        //  初始化 TLS
        ServicePointManager.SecurityProtocol =
            SecurityProtocolType.Tls12;
        Debug.Log(SystemInfo.operatingSystem);
    }
    void Start()
    {
        string[] args = System.Environment.GetCommandLineArgs();

        foreach (string arg in args)
        {
            if (arg == "-server")
            {
                Debug.Log("服务器模式启动");
                StartCoroutine(RegisterServer());
                NetworkManager.singleton.StartServer();
            }
        }
    }

    private IEnumerator RegisterServer()
    {
        string url = "https://meowgame.cloud/api/register";

        WWWForm form = new WWWForm();
        form.AddField("name", "我的房间");
        form.AddField("port", 7777);

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        www.certificateHandler = new IgnoreSSL();
        yield return www.SendWebRequest();
    }
}
