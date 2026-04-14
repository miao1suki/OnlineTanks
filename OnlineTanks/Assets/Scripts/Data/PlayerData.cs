using Mirror;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    [SyncVar(hook = nameof(OnColorChanged))]
    public int colorIndex;

    public SpriteRenderer[] renderers;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // 初始化
        OnColorChanged(colorIndex, colorIndex);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        CmdSetName(LocalPlayerData.PlayerName);
        CmdSetColor(LocalPlayerData.ColorIndex);
    }

    void OnNameChanged(string oldName, string newName)
    {
        Debug.Log($"名字更新: {newName}");
    }

    void OnColorChanged(int oldIndex, int newIndex)
    {
        if (newIndex < 0 || newIndex >= PlayerColorConfig.Colors.Length)
            return;

        ApplyColor(PlayerColorConfig.Colors[newIndex]);
    }

    [Command]
    public void CmdSetName(string name)
    {
        playerName = name;
    }

    [Command]
    public void CmdSetColor(int index)
    {
        if (index < 0 || index >= PlayerColorConfig.Colors.Length)
            return;

        colorIndex = index;
    }

    void ApplyColor(Color color)
    {
        foreach (var sr in renderers)
        {
            if (sr != null)
                sr.color = color;
        }
    }
}