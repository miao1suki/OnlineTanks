using UnityEngine;
using UnityEngine.UI;

public class VibrationToggleUI : MonoBehaviour
{
    public Toggle toggle;
    void OnEnable()
    {
        SettingData.OnVibrationChanged += SyncUI;
    }

    void OnDisable()
    {
        SettingData.OnVibrationChanged -= SyncUI;
    }
    void Start()
    {
        toggle.isOn = SettingData.VibrationEnabled;

        toggle.onValueChanged.AddListener(OnChanged);
    }

    void OnChanged(bool value)
    {
        SettingData.VibrationEnabled = value;
    }

    void SyncUI(bool value)
    {
        // 防止事件触发时递归调用
        toggle.SetIsOnWithoutNotify(value);
    }
}