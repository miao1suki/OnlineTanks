using UnityEngine;

public static class PlayerColorConfig
{
    public static readonly Color[] Colors = new Color[]
    {
        HexToColor("0dcb31"),
        HexToColor("cd3428"),
        HexToColor("1e68d7"),
        HexToColor("ebe42d"),
        HexToColor("a82dd5"),
        HexToColor("e47821")
    };

    public static Color HexToColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}
