using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KickToastUI : MonoBehaviour
{
    public static KickToastUI Instance;

    [Header("Lobby提示文字")]
    public TMP_Text lobbyText;

    [Header("Game提示文字")]
    public TMP_Text gameText;

    [Header("默认显示总时间")]
    public float totalDuration = 6f;

    [Header("默认淡出时间")]
    public float fadeDuration = 2f;

    Coroutine lobbyRoutine;
    Coroutine gameRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitText(lobbyText);
        InitText(gameText);
    }

    void InitText(TMP_Text text)
    {
        if (text == null) return;

        text.text = "";
        Color c = text.color;
        c.a = 0;
        text.color = c;
    }

    // 默认 Lobby
    public void Show(string msg)
    {
        Show(msg, UIContext.Lobby, totalDuration, fadeDuration);
    }

    public void Show(string msg, UIContext context)
    {
        Show(msg, context, totalDuration, fadeDuration);
    }

    public void Show(string msg, UIContext context, float totalTime, float fadeTime)
    {
        TMP_Text target = GetTargetText(context);
        if (target == null) return;

        // 选对应 coroutine
        if (context == UIContext.Lobby)
        {
            if (lobbyRoutine != null)
                StopCoroutine(lobbyRoutine);

            lobbyRoutine = StartCoroutine(FadeRoutine(msg, target, totalTime, fadeTime));
        }
        else
        {
            if (gameRoutine != null)
                StopCoroutine(gameRoutine);

            gameRoutine = StartCoroutine(FadeRoutine(msg, target, totalTime, fadeTime));
        }
    }

    TMP_Text GetTargetText(UIContext context)
    {
        return context switch
        {
            UIContext.Lobby => lobbyText,
            UIContext.Game => gameText,
            _ => lobbyText
        };
    }

    ref Coroutine GetRoutineRef(UIContext context)
    {
        return ref (context == UIContext.Game ? ref gameRoutine : ref lobbyRoutine);
    }

    IEnumerator FadeRoutine(string msg, TMP_Text target, float totalTime, float fadeTime)
    {
        target.text = msg;

        float holdTime = Mathf.Max(0, totalTime - fadeTime);

        Color c = target.color;
        c.a = 1f;
        target.color = c;

        // ===== 1. 全亮阶段 =====
        if (holdTime > 0)
            yield return new WaitForSeconds(holdTime);

        // ===== 2. 淡出阶段 =====
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;

            float alpha = Mathf.Lerp(1f, 0f, t / fadeTime);

            c = target.color;
            c.a = alpha;
            target.color = c;

            yield return null;
        }

        target.text = "";

        c.a = 0;
        target.color = c;

        if (target == lobbyText)
            lobbyRoutine = null;
        else
            gameRoutine = null;
    }
}

public enum UIContext
{
    Lobby,
    Game
}