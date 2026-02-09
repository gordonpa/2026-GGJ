using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 游戏结束结算 UI：显示获胜方、赢家图标、排行榜。倒计时结束时由 GameManager 触发；支持调试快捷显示。
/// 挂到 Canvas 下结算面板根物体，绑定 winnerText、winnerIcon、leaderboardText 等。
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("面板")]
    [Tooltip("结算面板根（显示/隐藏此物体）。不填则使用本脚本所在物体")]
    [SerializeField] private GameObject panelRoot;

    [Header("获胜信息")]
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private Image winnerIcon;
    [Tooltip("监管者胜利时显示的图标")]
    [SerializeField] private Sprite catcherWinSprite;
    [Tooltip("求生者胜利时显示的图标")]
    [SerializeField] private Sprite runnerWinSprite;

    [Header("排行榜")]
    [Tooltip("显示排行榜的文本（多行）")]
    [SerializeField] private TMP_Text leaderboardText;

    /// <summary>实际控制显示/隐藏的对象：未指定 panelRoot 时用当前物体。</summary>
    private GameObject EffectivePanel => panelRoot != null ? panelRoot : gameObject;

    private void Awake()
    {
        GameManager.OnGameOver += HandleGameOver;
    }

    private void OnDestroy()
    {
        GameManager.OnGameOver -= HandleGameOver;
    }

    private void Start()
    {
        EffectivePanel.SetActive(false);
    }

    private void HandleGameOver(bool catcherWin)
    {
        Debug.Log("[GameOverUI] HandleGameOver 收到事件，catcherWin=" + catcherWin);
        Show(catcherWin, null);
    }

    /// <summary>显示结算界面：catcherWin=true 为监管者胜，winnerSprite 为空则用配置的默认图标。</summary>
    public void Show(bool catcherWin, Sprite winnerSprite = null)
    {
        GameObject panel = EffectivePanel;
        if (panel == null)
        {
            Debug.LogWarning("[GameOverUI] EffectivePanel 为空，无法显示");
            return;
        }

        if (winnerText != null)
            winnerText.text = catcherWin ? "" : "";

        if (winnerIcon != null)
        {
            Sprite s = winnerSprite != null ? winnerSprite : (catcherWin ? catcherWinSprite : runnerWinSprite);
            winnerIcon.enabled = s != null;
            if (s != null) winnerIcon.sprite = s;
        }

        if (leaderboardText != null)
        {
            var entries = GameOverLeaderboardProvider.GetEntries();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < entries.Count; i++)
                sb.AppendLine($"{i + 1}. {entries[i].displayName}  {entries[i].score}");
            leaderboardText.text = sb.Length > 0 ? sb.ToString() : "（暂无排行榜）";
        }

        for (Transform p = panel.transform; p != null; p = p.parent)
            p.gameObject.SetActive(true);
        Debug.Log("[GameOverUI] 已 SetActive(true) 面板及其父级: " + panel.name);
    }

    /// <summary>隐藏结算面板。</summary>
    public void Hide()
    {
        EffectivePanel.SetActive(false);
    }

    /// <summary>调试用：不经过 GameManager，直接显示结算界面（可传入排行榜和赢家图标）。</summary>
    public void ShowForDebug(bool catcherWin, IList<(string displayName, int score)> entries, Sprite winnerSprite = null)
    {
        if (entries != null)
            GameOverLeaderboardProvider.SetEntries(entries);
        else
            GameOverLeaderboardProvider.SetEntries(new List<(string, int)>());
        Show(catcherWin, winnerSprite);
    }
}
