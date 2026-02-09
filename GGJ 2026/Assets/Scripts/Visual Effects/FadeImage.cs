using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeImage : MonoBehaviour
{
    public Image targetImage;
    public float fadeDuration = 2.0f;

    public float x = 0.0f;

    public float y = 1.0f;

    public void StartFade(float fromAlpha, float toAlpha)
    {
        StartCoroutine(FadeCoroutine(fromAlpha, toAlpha, fadeDuration));
    }

    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color color = targetImage.color;

        color.a = startAlpha;
        targetImage.color = color;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            targetImage.color = color;

            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        color.a = endAlpha;
        targetImage.color = color;
    }

    void Start()
    {
        // 让图片从完全透明(0)变为完全不透明(1)，耗时2秒
        StartFade(x, y);
        
        // 或者从半透明(0.5)到透明(0)，耗时fadeDuration变量指定的时间
        // StartFade(0.5f, 0f);
    }
}
