using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 监管者（追逐者）可移动倒计时可视化。与现有倒计时风格类似，可放在不同位置（挂到任意 UI 节点即可）。
/// 仅当本地玩家为追逐者且处于开局冻结时显示，显示剩余秒数与圆形进度。
/// </summary>
public class ChaserFreezeCountdownUI : MonoBehaviour
{
    [Header("UI 组件绑定")]
    [Tooltip("圆形进度条（Image 类型 Fill）")]
    public Image circleProgress;
    [Tooltip("剩余时间文字")]
    public TextMeshProUGUI timeText;
    [Tooltip("低时间闪烁效果（可选）")]
    public CanvasGroup flashEffect;

    [Header("进度条参考时长")]
    [Tooltip("用于计算 fill 的参考时长（秒），需与 LobbyManager 的 chaserFreezeSeconds 一致")]
    public float expectedFreezeDuration = 5f;

    [Header("低时间阈值与样式")]
    [Tooltip("剩余多少秒时变红并脉冲")]
    public float lowTimeThreshold = 2f;
    public float pulseSpeed = 3f;
    public float pulseScale = 1.1f;

    [Header("颜色（可选）")]
    public Color normalColor = Color.white;
    public Color lowTimeColor = Color.red;

    private FactionMember localFaction;
    private Vector3 originalTextScale;
    private bool isPulsing;
    private float lastRemaining = -1f;

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            SetVisible(false);
            return;
        }
        if (localFaction == null)
        {
            var po = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (po != null)
                localFaction = po.GetComponent<FactionMember>();
        }
        if (localFaction == null || !localFaction.IsChaser)
        {
            SetVisible(false);
            return;
        }
        double until = localFaction.ChaserFreezeUntil;
        if (until <= 0)
        {
            SetVisible(false);
            return;
        }
        double now = NetworkManager.Singleton.ServerTime.Time;
        if (now >= until)
        {
            SetVisible(false);
            return;
        }
        float remaining = (float)(until - now);
        SetVisible(true);
        UpdateTimeDisplay(remaining);
        UpdateProgressVisual(remaining);
    }

    private void SetVisible(bool visible)
    {
        if (circleProgress != null) circleProgress.gameObject.SetActive(visible);
        if (timeText != null) timeText.gameObject.SetActive(visible);
        if (flashEffect != null) flashEffect.gameObject.SetActive(visible);
    }

    private void UpdateTimeDisplay(float remaining)
    {
        if (timeText == null) return;
        if (originalTextScale == Vector3.zero) originalTextScale = timeText.transform.localScale;

        int sec = Mathf.Max(0, Mathf.CeilToInt(remaining));
        timeText.text = sec <= 0 ? "0" : $"{sec}";

        if (remaining < lowTimeThreshold)
        {
            timeText.color = lowTimeColor;
            if (!isPulsing)
            {
                isPulsing = true;
            }
        }
        else
        {
            timeText.color = normalColor;
            isPulsing = false;
            timeText.transform.localScale = originalTextScale;
        }
        lastRemaining = remaining;
    }

    private void UpdateProgressVisual(float remaining)
    {
        if (circleProgress != null && expectedFreezeDuration > 0)
        {
            float progress = Mathf.Clamp01(remaining / expectedFreezeDuration);
            circleProgress.fillAmount = progress;
        }
        if (isPulsing && timeText != null && originalTextScale != Vector3.zero)
        {
            float t = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            timeText.transform.localScale = originalTextScale * Mathf.Lerp(1f, pulseScale, t);
            if (flashEffect != null) flashEffect.alpha = t * 0.3f;
        }
        else if (flashEffect != null)
            flashEffect.alpha = 0f;
    }
}
