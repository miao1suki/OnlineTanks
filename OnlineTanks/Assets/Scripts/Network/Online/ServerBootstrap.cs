using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

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
                StartCoroutine(RegisterServer());
                NetworkManager.singleton.StartServer();
            }
        }
    }

    private IEnumerator RegisterServer()
    {
        string url = "http://meowgame.cloud/api/register";

        WWWForm form = new WWWForm();
        form.AddField("name", "我的房间");
        form.AddField("port", 7777);

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        yield return www.SendWebRequest();
    }
}
