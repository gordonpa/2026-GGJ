using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可选调试：不等待倒计时，输入输赢和三人排行榜后点击「显示结算」弹出结算 UI。
/// 挂到场景任意物体即可；运行时在屏幕一角显示调试面板。
/// </summary>
public class GameOverDebugPanel : MonoBehaviour
{
    [Header("可选")]
    [Tooltip("不填则运行时 FindObjectOfType 查找")]
    [SerializeField] private GameOverUI gameOverUI;

    [Header("调试面板位置（屏幕比例 0~1）")]
    [SerializeField] private Vector2 positionPercent = new Vector2(0.02f, 0.02f);
    [SerializeField] private float width = 320f;
    [SerializeField] private float rowHeight = 22f;

    private bool _catcherWin = false;
    private readonly string[] _names = new string[3] { "玩家1", "玩家2", "玩家3" };
    private readonly string[] _scores = new string[3] { "100", "80", "60" };
    private bool _showPanel = true;

    private void OnGUI()
    {
        if (gameOverUI == null) gameOverUI = FindObjectOfType<GameOverUI>(true);
        if (gameOverUI == null) return;

        float x = Screen.width * positionPercent.x;
        float y = Screen.height * positionPercent.y;

        GUILayout.BeginArea(new Rect(x, y, width, 400));
        GUILayout.Label("【结算调试】");
        _showPanel = GUILayout.Toggle(_showPanel, "显示调试面板");

        if (_showPanel)
        {
            GUILayout.Label("获胜方：");
            _catcherWin = GUILayout.Toggle(_catcherWin, " 监管者胜（追逐者赢）");

            GUILayout.Space(4);
            GUILayout.Label("排行榜（3 人）：");
            for (int i = 0; i < 3; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"名{i + 1}", GUILayout.Width(28));
                _names[i] = GUILayout.TextField(_names[i], GUILayout.Width(100));
                GUILayout.Label("分", GUILayout.Width(14));
                _scores[i] = GUILayout.TextField(_scores[i], GUILayout.Width(60));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
            if (GUILayout.Button("显示结算", GUILayout.Height(28)))
            {
                var entries = new List<(string displayName, int score)>();
                for (int i = 0; i < 3; i++)
                {
                    int score = 0;
                    int.TryParse(_scores[i], out score);
                    entries.Add((_names[i] ?? $"玩家{i + 1}", score));
                }
                gameOverUI.ShowForDebug(_catcherWin, entries, null);
            }
        }

        GUILayout.EndArea();
    }
}
