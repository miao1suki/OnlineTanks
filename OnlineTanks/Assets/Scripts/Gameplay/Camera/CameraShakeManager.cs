using System.Collections;
using UnityEngine;

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    [Header("Camera Ref (optional)")]
    public Camera targetCamera;

    [Header("Mobile Vibration")]
    bool EnableVibration => SettingData.VibrationEnabled;

    Transform camTf;
    Vector3 originalPos;
    Coroutine shakeCo;

    void Awake()
    {
        // Game 场景单例：不DontDestroyOnLoad，避免出现在Lobby
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            camTf = targetCamera.transform;
            originalPos = camTf.localPosition;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Shake(float duration, float strength)
    {
        if (duration <= 0f || strength <= 0f) return;

        // 相机可能还没 ready（比如场景刚进来）
        if (camTf == null)
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null)
            {
                camTf = targetCamera.transform;
                originalPos = camTf.localPosition;
            }
        }

        // 手机震动（最简API：无法自定义强度/时长）
#if UNITY_ANDROID || UNITY_IOS
if (EnableVibration)
{
    UnityEngine.Handheld.Vibrate();
}
#endif

        if (camTf == null) return;

        if (shakeCo != null)
            StopCoroutine(shakeCo);

        shakeCo = StartCoroutine(ShakeRoutine(duration, strength));
    }

    IEnumerator ShakeRoutine(float duration, float strength)
    {
        float t = 0f;

        // 记录开始位置（防止相机被别的系统移动后抖动回错位）
        originalPos = camTf.localPosition;

        while (t < duration)
        {
            t += Time.deltaTime;

            // 衰减，让尾巴自然一点（可删）
            float k = 1f - (t / duration);

            Vector2 offset2 = Random.insideUnitCircle * strength * k;
            camTf.localPosition = originalPos + new Vector3(offset2.x, offset2.y, 0f);

            yield return null;
        }

        camTf.localPosition = originalPos;
        shakeCo = null;
    }
}