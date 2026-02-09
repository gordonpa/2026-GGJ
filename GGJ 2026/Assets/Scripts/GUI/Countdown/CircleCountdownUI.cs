using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(NetworkCountdownTimer))]
public class CircleCountdownUI : MonoBehaviour
{
    [Header("UI组件绑定")]
    public Image circleProgress;
    public TextMeshProUGUI timeText;
    public CanvasGroup flashEffect;

    [Header("GameManager（用于获取游戏阶段）")]
    [Tooltip("拖入GameManager，用于区分准备阶段(黄色)和游戏阶段(白色/红色)")]
    public GameManager gameManager;

    [Header("颜色配置（仅Fill变色）")]
    public Gradient colorGradient;

    [Header("动画设置")]
    public float pulseSpeed = 3f;
    public float pulseScale = 1.1f;

    private NetworkCountdownTimer timer;
    private Vector3 originalScale;
    private bool isPulsing = false;
    private Coroutine pulseCoroutine;
    private float lowTimeThreshold;

    void Awake()
    {
        timer = GetComponent<NetworkCountdownTimer>();
        lowTimeThreshold = timer.LowTimeThreshold;

        // 订阅事件
        timer.OnTimeChanged += UpdateTimeDisplay;
        timer.OnProgressChanged += UpdateProgressVisual;
        timer.OnTimeUp += HandleTimeUp;

        if (timeText != null)
            originalScale = timeText.transform.localScale;
    }

    void OnDestroy()
    {
        if (timer != null)
        {
            timer.OnTimeChanged -= UpdateTimeDisplay;
            timer.OnProgressChanged -= UpdateProgressVisual;
            timer.OnTimeUp -= HandleTimeUp;
        }
    }

    void UpdateTimeDisplay(float time)
    {
        if (timeText == null) return;

        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int milliseconds = Mathf.FloorToInt((time * 100) % 100);

        // ===== 准备阶段（5秒倒计时）=====
        // 显示黄色，"准备 05.00"（保留中文提示，但数字部分是4位）
        if (gameManager != null && gameManager.CurrentState == GameManager.GameState.ReadyCountdown)
        {
            timeText.text = $"准备 {seconds:00}.{milliseconds:00}";
            timeText.color = Color.yellow;

            if (!isPulsing)
            {
                isPulsing = true;
                pulseCoroutine = StartCoroutine(PulseAnimation());
            }

            if (circleProgress != null)
            {
                circleProgress.fillAmount = time / gameManager.readyTime;
                circleProgress.color = Color.yellow;
            }
            return;
        }

        // ===== 游戏阶段（全部显示4位数字：秒.毫秒）=====
        // 不再区分"最后10秒"和"正常时间"，统一显示格式：SS.MM
        timeText.text = $"{seconds:00}.{milliseconds:00}";

        // 最后10秒变红色 + 脉冲，其他时间白色
        if (time < lowTimeThreshold)
        {
            timeText.color = Color.red;
            if (!isPulsing)
            {
                isPulsing = true;
                pulseCoroutine = StartCoroutine(PulseAnimation());
            }
        }
        else
        {
            timeText.color = Color.white;
            if (isPulsing)
            {
                isPulsing = false;
                if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                timeText.transform.localScale = originalScale;
                if (flashEffect != null) flashEffect.alpha = 0;
            }
        }
    }

    void UpdateProgressVisual(float progress)
    {
        if (circleProgress == null) return;

        // 准备阶段在上面已经处理了颜色和fill，这里只处理游戏阶段
        if (gameManager != null && gameManager.CurrentState == GameManager.GameState.ReadyCountdown)
            return;

        circleProgress.fillAmount = progress;
        circleProgress.color = colorGradient.Evaluate(1 - progress);
    }

    IEnumerator PulseAnimation()
    {
        while (isPulsing)
        {
            float t = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            float scale = Mathf.Lerp(1f, pulseScale, t);
            timeText.transform.localScale = originalScale * scale;

            if (flashEffect != null)
                flashEffect.alpha = t * 0.3f;

            yield return null;
        }

        timeText.transform.localScale = originalScale;
        if (flashEffect != null) flashEffect.alpha = 0;
    }

    void HandleTimeUp()
    {
        StartCoroutine(TimeUpFlash());
    }

    IEnumerator TimeUpFlash()
    {
        // 停止脉冲动画
        isPulsing = false;
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);

        // 保持显示 4位数字 00.00
        timeText.text = "00.00";
        timeText.color = Color.red;
        timeText.transform.localScale = originalScale;

        // 闪烁提示
        for (int i = 0; i < 3; i++)
        {
            timeText.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            timeText.color = Color.red;
            yield return new WaitForSeconds(0.1f);
        }

        // 最终保持红色 00.00（4位数字）
        timeText.text = "00.00";
        timeText.color = Color.red;
    }
}