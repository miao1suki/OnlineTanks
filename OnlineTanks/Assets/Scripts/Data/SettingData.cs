using UnityEngine;

public static class SettingData
{
    public static System.Action<bool> OnVibrationChanged;

    public static bool VibrationEnabled
    {
        get => PlayerPrefs.GetInt("vibration", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("vibration", value ? 1 : 0);
            OnVibrationChanged?.Invoke(value);
        }
    }
}