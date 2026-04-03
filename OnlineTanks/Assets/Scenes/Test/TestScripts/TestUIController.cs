using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class TestUIController : MonoBehaviour
{
    public Text statusText;
    public Button connectButton;
    public Button disconnectButton;

    void Start()
    {
        // 初始状态
        UpdateStatus(false);

        connectButton.onClick.AddListener(OnClickConnect);
        disconnectButton.onClick.AddListener(OnClickDisconnect);

        TestNetworkManager.OnConnectionStatusChanged += UpdateStatus;
    }

    void OnDestroy()
    {
        TestNetworkManager.OnConnectionStatusChanged -= UpdateStatus;
    }

    void OnClickConnect()
    {
        Debug.Log("尝试连接服务器...");

        NetworkManager.singleton.networkAddress = "meowgame.cloud"; // 云服务器IP
        NetworkManager.singleton.StartClient();
    }

    void OnClickDisconnect()
    {
        Debug.Log("断开连接");

        NetworkManager.singleton.StopClient();
        UpdateStatus(false);
    }

    void UpdateStatus(bool connected)
    {
        statusText.text = connected ? "已连接服务器" : "未连接服务器";
    }
}
