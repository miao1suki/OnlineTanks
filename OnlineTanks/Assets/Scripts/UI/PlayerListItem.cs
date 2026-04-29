using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour
{
    public TMP_Text playerNameText;

    public Button kickButton;

    PlayerController target;

    public void Bind(PlayerController p)
    {
        target = p;

        var data = p.GetComponent<PlayerData>();

        PlayerController local =
            NetworkClient.localPlayer?.GetComponent<PlayerController>();

        if (local == null)
            return;

        bool localIsHost =
            NetworkServer.active && NetworkClient.active;

        bool targetIsLocal = p.isLocalPlayer;

        bool targetIsHost =
            p.isHostPlayer; 

        bool canKick = false;

        if (localIsHost)
        {
            // Host：不能踢自己
            canKick = !targetIsLocal;
        }
        else
        {
            // 普通玩家：
            // 不能踢自己 + 不能踢 host
            canKick = !targetIsLocal && !targetIsHost;
        }

        kickButton.gameObject.SetActive(canKick);

        kickButton.onClick.RemoveAllListeners();

        if (canKick)
            kickButton.onClick.AddListener(KickPlayer);

        if (data != null && !string.IsNullOrEmpty(data.playerName))
        {
            playerNameText.text = data.playerName;
        }

        data.OnReady += RefreshName;
    }

    void RefreshName()
    {
        var data = target.GetComponent<PlayerData>();
        if (data != null)
            playerNameText.text = data.playerName;
    }

    void KickPlayer()
    {
        if (target == null)
            return;

        PlayerController local =
            NetworkClient.localPlayer
                .GetComponent<PlayerController>();

        if (local == null)
            return;

        local.CmdKickPlayer(target.netId);
    }

    void OnDestroy()
    {
        if (target != null)
        {
            var data = target.GetComponent<PlayerData>();
            if (data != null)
                data.OnReady -= RefreshName;
        }
    }
}