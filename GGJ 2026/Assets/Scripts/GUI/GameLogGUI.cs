using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在 GUI 上显示最近若干条 log，供 ChaserShockwave、掉落面具等调试用。任意脚本可调用 GameLogGUI.AddLine(msg)。
/// 挂到场景中任意物体上即可显示；不挂则只打 Console，不显示在 GUI。
/// </summary>
public class GameLogGUI : MonoBehaviour
{
    [Header("显示位置（屏幕比例 0-1，左上角为 (0,1)）")]
    [SerializeField] private Vector2 positionPercent = new Vector2(0.02f, 0.6f);
    [SerializeField] private Vector2 sizePercent = new Vector2(0.5f, 0.4f);

    [Header("条数")]
    [SerializeField] private int maxLines = 25;

    [Header("样式")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.9f);
    [SerializeField] private Color textColor = new Color(0.9f, 0.9f, 0.95f);
    [SerializeField] private Color warningColor = new Color(1f, 0.75f, 0.3f);

    private static readonly List<string> _lines = new List<string>();
    private static readonly object _lock = new object();
    private static GameLogGUI _instance;

    private GUIStyle _boxStyle;
    private GUIStyle _labelStyle;
    private bool _stylesInitialized;
    private string _cachedText = "";
    private int _cachedCount = -1;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>往 GUI 日志里追加一行（任意脚本可调），仅显示在 GUI 不写 Console。</summary>
    public static void AddLine(string message)
    {
        lock (_lock)
        {
            _lines.Add(message);
            while (_lines.Count > 50)
                _lines.RemoveAt(0);
        }
    }

    /// <summary>追加一行并标记为警告（GUI 里可区分颜色），仅显示在 GUI 不写 Console。</summary>
    public static void AddWarning(string message)
    {
        lock (_lock)
        {
            _lines.Add("[!] " + message);
            while (_lines.Count > 50)
                _lines.RemoveAt(0);
        }
    }

    private void OnGUI()
    {
        if (!EnsureStyles()) return;

        lock (_lock)
        {
            // 显示最新的 maxLines 条（从后往前取）
            int start = Mathf.Max(0, _lines.Count - maxLines);
            int count = _lines.Count - start;
            
            // 每次都重新生成文本，确保显示最新内容
            _cachedText = "";
            for (int i = start; i < _lines.Count; i++)
                _cachedText += _lines[i] + "\n";
            _cachedCount = count;
        }

        float w = Screen.width * sizePercent.x;
        float h = Screen.height * sizePercent.y;
        float x = Screen.width * positionPercent.x;
        float y = Screen.height * (1f - positionPercent.y) - h;

        GUI.Box(new Rect(x, y, w, h), "", _boxStyle);
        GUI.Label(new Rect(x + 6, y + 6, w - 12, h - 12), _cachedText, _labelStyle);
    }

    private bool EnsureStyles()
    {
        if (_stylesInitialized) return true;
        _boxStyle = new GUIStyle(GUI.skin.box);
        _boxStyle.normal.background = MakeTex(2, 2, backgroundColor);
        _labelStyle = new GUIStyle(GUI.skin.label);
        _labelStyle.normal.textColor = textColor;
        _labelStyle.fontSize = Mathf.Max(11, (int)(12 * (Screen.height / 540f)));
        _labelStyle.wordWrap = true;
        _stylesInitialized = true;
        return true;
    }

    private static Texture2D MakeTex(int w, int h, Color c)
    {
        var tex = new Texture2D(w, h);
        for (int i = 0; i < w * h; i++) tex.SetPixel(i % w, i / w, c);
        tex.Apply();
        return tex;
    }
}
