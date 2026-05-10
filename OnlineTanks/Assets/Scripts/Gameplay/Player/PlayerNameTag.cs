using Mirror;
using TMPro;
using UnityEngine;

public class PlayerNameTag : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text nameText;

    [Header("跟随偏移（世界坐标）")]
    public Vector3 worldOffset = new Vector3(0, 3.2f, 0);

    [Header("是否一直面向摄像机（2D一般不需要）")]
    public bool faceCamera = false;

    PlayerData data;
    PlayerController controller;

    void Awake()
    {
        data = GetComponentInParent<PlayerData>();
        controller = GetComponentInParent<PlayerController>();

        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>(true);
    }

    void OnEnable()
    {
        if (data != null)
            data.OnReady += RefreshName;

        RefreshName();
    }

    void OnDisable()
    {
        if (data != null)
            data.OnReady -= RefreshName;
    }

    void RefreshName()
    {
        if (nameText == null || data == null) return;
        nameText.text = string.IsNullOrWhiteSpace(data.playerName) ? "Player" : data.playerName;
    }

    void LateUpdate()
    {
        if (data == null || controller == null) return;

        // 只在Playing且活着显示
        bool playing = (MatchManager.Instance != null &&
                        MatchManager.Instance.currentState == RoomState.Playing);

        bool visible = playing && controller.isAlive;

        if (nameText != null)
            nameText.enabled = visible;

        if (!visible) return;

        // 跟随位置（不继承旋转）
        Transform owner = controller.transform;
        transform.position = owner.position + worldOffset;

        // 不旋转（2D）
        if (!faceCamera)
        {
            transform.rotation = Quaternion.identity;
        }
        else
        {
            var cam = Camera.main;
            if (cam != null)
                transform.forward = cam.transform.forward;
        }
    }
}