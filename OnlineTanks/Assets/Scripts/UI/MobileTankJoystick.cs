using UnityEngine;
using UnityEngine.EventSystems;


[DefaultExecutionOrder(-200)]
public class MobileTankJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI")]
    public RectTransform bg;
    public RectTransform handle;

    [Header("Output")]
    public PlayerInputHandler targetInput; // 拖本地玩家身上的 PlayerInputHandler

    [Header("Tuning")]
    public float radius = 90f;             // 手柄最大位移（像素）
    public float deadZone = 0.08f;         // 死区
    public bool normalize = true;          // 输出限制到[-1,1]
    public float smooth = 20f;             // 回中/跟随平滑（越大越跟手）

    Vector2 raw;                           // -1..1
    Vector2 current;                       // 平滑后输出
    Canvas canvas;
    Camera uiCam;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera;

        if (bg == null) bg = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (targetInput == null)
            targetInput = PlayerInputHandler.Local;

        current = Vector2.Lerp(
            current,
            raw,
            smooth * Time.unscaledDeltaTime
        );

        if (targetInput != null)
        {
            float strength =
                Mathf.Clamp01(current.magnitude);

            // 只用Y表示前进力度
            targetInput.MoveInput =
                new Vector2(0, strength);

            // 摇杆方向 = 坦克朝向
            if (current.sqrMagnitude > 0.01f)
            {
                targetInput.LookInput =
                    current.normalized;

                targetInput.IsMobileLook = true;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TryAutoBind();
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (bg == null || handle == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bg, eventData.position, uiCam, out var localPos);

        // localPos 是以 bg 中心为原点的像素坐标（取决于 pivot）
        // 假设 bg pivot 在中心；如果不是，建议把 pivot 设为(0.5,0.5)
        Vector2 v = localPos;

        // 限制到半径
        Vector2 clamped = Vector2.ClampMagnitude(v, radius);
        handle.anchoredPosition = clamped;

        // 转成 -1..1
        Vector2 out01 = clamped / radius;

        // 死区
        if (out01.magnitude < deadZone)
            out01 = Vector2.zero;

        raw = normalize ? Vector2.ClampMagnitude(out01, 1f) : out01;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        raw = Vector2.zero;

        if (targetInput != null)
        {
            targetInput.MoveInput = Vector2.zero;
        }

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    void TryAutoBind()
    {
        // 永远绑定到本地玩家的输入
        if (targetInput == null)
            targetInput = PlayerInputHandler.Local;
    }

    public void ResetJoystick()
    {
        raw = Vector2.zero;
        current = Vector2.zero;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (targetInput != null)
        {
            targetInput.MoveInput = Vector2.zero;

            // 重置朝向为正上
            targetInput.LookInput = Vector2.up;

            targetInput.IsMobileLook = true;
        }
    }
}