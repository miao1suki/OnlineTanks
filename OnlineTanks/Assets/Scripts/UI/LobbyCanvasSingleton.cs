using UnityEngine;

public class LobbyCanvasSingleton : MonoBehaviour
{
    public static LobbyCanvasSingleton Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 销毁新来的（Lobby 场景里的）
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 让 GameManager 永远拿到正确引用
        if (GameManager.instance != null)
            GameManager.instance.LobbyCanvas = gameObject;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}