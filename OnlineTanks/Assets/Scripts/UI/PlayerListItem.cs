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

        PlayerData data =
            p.GetComponent<PlayerData>();

        if (data != null)
            playerNameText.text =
                data.playerName;
        else
            playerNameText.text =
                "Player";

        kickButton.gameObject.SetActive(!p.isLocalPlayer);

        kickButton.onClick.RemoveAllListeners();
        if(!p.isLocalPlayer)
        {
            kickButton.onClick.AddListener(KickPlayer);
        }

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