using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Mirror;
using kcp2k;

public class ServerHeartbeat : MonoBehaviour
{
    bool isRunning;

    void Start()
    {
        StartCoroutine(WaitForServer());
    }

    IEnumerator WaitForServer()
    {
        // 等待服务器真正启动
        while (!NetworkServer.active)
        {
            yield return null;
        }

        isRunning = true;
        StartCoroutine(HeartbeatLoop());
    }

    IEnumerator HeartbeatLoop()
    {
        while (isRunning)
        {
            yield return SendHeartbeat();
            yield return new WaitForSeconds(10f);
        }
    }

    IEnumerator SendHeartbeat()
    {
        string url = "https://62.234.93.20/api/heartbeat";

        var transport = NetworkManager.singleton.transport;

        WWWForm form = new WWWForm();
        form.AddField("port", GetPort(transport));

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        www.certificateHandler = new IgnoreSSL();
        yield return www.SendWebRequest();
    }

    int GetPort(Transport transport)
    {
        if (transport is KcpTransport kcp)
            return kcp.Port;

        if (transport is TelepathyTransport tel)
            return tel.port;

        return 7777;
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        StartCoroutine(RemoveRoom());
    }

    IEnumerator RemoveRoom()
    {
        string url = "https://62.234.93.20/api/removeRoom";

        var transport = NetworkManager.singleton.transport;

        WWWForm form = new WWWForm();
        form.AddField("port", GetPort(transport));

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        www.certificateHandler = new IgnoreSSL();
        yield return www.SendWebRequest();
    }
}