using System.Collections;
using TMPro;
using UnityEngine;

public class TextAnim : MonoBehaviour
{
    public TMP_Text titleText;
    public TextAnim anim;
    private void OnEnable()
    {
        StartCoroutine(anim.PlaySpacingAnim(titleText));
    }
    private void OnDisable()
    {
        StopCoroutine(anim.PlaySpacingAnim(titleText));
    }
    public IEnumerator PlaySpacingAnim(TMP_Text text)
    {
        if (text == null) yield break;

        // 第一段：1600 → -0.3（1秒）
        yield return StartCoroutine(LerpSpacing(text, 1600f, -0.3f, 1f));

        // 第二段：-10.1 → 0（0.3秒）
        yield return StartCoroutine(LerpSpacing(text, -10.1f, 0f, 0.3f));
    }

    IEnumerator LerpSpacing(TMP_Text text, float start, float end, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            t = Mathf.SmoothStep(0, 1, t);

            float value = Mathf.Lerp(start, end, t);
            text.characterSpacing = value;

            yield return null;
        }

        // 保证最后精确到目标值
        text.characterSpacing = end;
    }
}