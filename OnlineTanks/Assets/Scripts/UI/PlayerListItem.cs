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

        kickButton.gameObject.SetActive(!p.isLocalPlayer);

        kickButton.onClick.RemoveAllListeners();
        if (!p.isLocalPlayer)
        {
            kickButton.onClick.AddListener(KickPlayer);
        }

        if (data != null)
        {
            if (!string.IsNullOrEmpty(data.playerName))
            {
                playerNameText.text = data.playerName;
            }
            else
            {
                data.OnReady += RefreshName;
            }
        }

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
}