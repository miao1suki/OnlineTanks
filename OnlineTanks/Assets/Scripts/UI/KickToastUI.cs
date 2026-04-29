using System.Collections;
using TMPro;
using UnityEngine;

public class KickToastUI : MonoBehaviour
{
    public static KickToastUI Instance;

    [Header("提示文字")]
    public TMP_Text tipText;

    [Header("淡出时间")]
    public float fadeDuration = 3f;

    Coroutine fadeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        if (tipText != null)
        {
            tipText.text = "";

            Color c = tipText.color;
            c.a = 0;
            tipText.color = c;
        }
    }

    public void Show(string msg)
    {
        if (tipText == null)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine =
            StartCoroutine(
                FadeMessageRoutine(msg)
            );
    }

    IEnumerator FadeMessageRoutine(string msg)
    {
        tipText.text = msg;

        Color c = tipText.color;
        c.a = 1f;
        tipText.color = c;

        float t = 0;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;

            float alpha =
                Mathf.Lerp(
                    1f,
                    0f,
                    t / fadeDuration
                );

            c = tipText.color;
            c.a = alpha;
            tipText.color = c;

            yield return null;
        }

        tipText.text = "";

        c.a = 0;
        tipText.color = c;

        fadeRoutine = null;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}