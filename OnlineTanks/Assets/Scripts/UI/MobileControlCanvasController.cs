using UnityEngine;
using Mirror;

public class MobileControlCanvasController : MonoBehaviour
{
    [Header("Root (Joystick Canvas Root)")]
    public GameObject root;

    PlayerController localPlayer;
    bool lastActive;

    void Awake()
    {
        if (root == null) root = gameObject;
        SetActive(false);
    }

    void Update()
    {
        if (!IsMobilePlatform())
        {
            SetActive(false);
            return;
        }

        // 1) 必须在Game场景且MatchManager存在
        if (MatchManager.Instance == null)
        {
            SetActive(false);
            return;
        }

        // 2) 只有Playing阶段才允许开
        bool inPlaying = MatchManager.Instance.currentState == RoomState.Playing;
        if (!inPlaying)
        {
            SetActive(false);
            return;
        }

        // 3) 找到本地玩家（只找一次，找不到就继续尝试）
        if (localPlayer == null)
            localPlayer = FindLocalPlayer();

        if (localPlayer == null)
        {
            SetActive(false);
            return;
        }

        // 4) 本地玩家活着才显示
        SetActive(localPlayer.isAlive);
    }

    bool IsMobilePlatform()
    {
#if UNITY_ANDROID || UNITY_IOS
    return true;
#else
        return false;
#endif
    }

    PlayerController FindLocalPlayer()
    {
        var players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p != null && p.isLocalPlayer)
                return p;
        }
        return null;
    }

    void SetActive(bool b)
    {
        if (lastActive == b) return;
        lastActive = b;
        if (root != null) root.SetActive(b);
    }
}