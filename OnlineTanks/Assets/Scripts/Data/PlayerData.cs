using Mirror;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;
    [SyncVar(hook = nameof(OnColorChanged))]
    public int colorIndex;

    public SpriteRenderer[] renderers;

    // 初始化显示
    public override void OnStartClient()
    {
        base.OnStartClient();
        OnColorChanged(colorIndex, colorIndex);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        Debug.Log("本地玩家生成，发送名字");

        string name = UIController.LocalPlayerName;

        if (!string.IsNullOrWhiteSpace(name))
        {
            CmdSetName(name);
        }
        CmdSetColor(UIController.LocalColorIndex);
    }

    // 名字变化时（自动在客户端触发）
    void OnNameChanged(string oldName, string newName)
    {
        Debug.Log($"名字更新: {newName}");
        // 更新UI
    }

    [Command]
    public void CmdSetName(string name)
    {
        playerName = name;
    }

    // 颜色变化时
    void OnColorChanged(int oldIndex, int newIndex)
    {
        Color color = PlayerColorConfig.Colors[newIndex];

        ApplyColor(color);
    }

    [Command]
    public void CmdSetColor(int index)
    {
        if (colorIndex == index) return;

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
