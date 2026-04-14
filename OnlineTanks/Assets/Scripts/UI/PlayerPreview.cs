using UnityEngine;
using UnityEngine.UI;

public class PlayerPreview : MonoBehaviour
{
    public Image[] images; // UIͼƬ

    private void Start()
    {
        ApplyColor(0);

        ApplyColor(LocalPlayerData.ColorIndex);
    }

    public void ApplyColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= PlayerColorConfig.Colors.Length)
            return;

        Color color = PlayerColorConfig.Colors[colorIndex];

        foreach (var img in images)
        {
            if (img != null)
                img.color = color;
        }
    }
}