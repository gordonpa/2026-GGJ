using System.Collections.Generic;
using UnityEngine;

public class IngameDebugConsole : MonoBehaviour
{
    private struct Log
    {
        public string message;
        public string stackTrace;
        public LogType type;
    }

    private readonly List<Log> logs = new List<Log>();
    private Vector2 scrollPosition;
    private bool show;
    private bool collapse;

    // 限制日志数量，防止卡顿
    private const int maxLogs = 100;

    private static readonly Dictionary<LogType, Color> logTypeColors = new Dictionary<LogType, Color>
    {
        { LogType.Assert, Color.white },
        { LogType.Error, Color.red },
        { LogType.Exception, Color.red },
        { LogType.Log, Color.white },
        { LogType.Warning, Color.yellow },
    };

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        // 按 ~ 键 (Tab上面那个) 切换显示
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            show = !show;
        }
    }

    private void HandleLog(string message, string stackTrace, LogType type)
    {
        logs.Add(new Log
        {
            message = message,
            stackTrace = stackTrace,
            type = type,
        });

        if (logs.Count > maxLogs)
        {
            logs.RemoveAt(0);
        }

        // 自动滚动到底部
        scrollPosition.y = float.MaxValue;
    }

    private void OnGUI()
    {
        if (!show) return;

        // 绘制半透明背景
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height * 0.5f), "Debug Console (~ to close)");

        // 绘制工具栏
        GUILayout.BeginArea(new Rect(10, 20, Screen.width - 20, Screen.height * 0.5f - 20));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear", GUILayout.Width(100))) logs.Clear();
        collapse = GUILayout.Toggle(collapse, "Collapse", GUILayout.Width(100));
        GUILayout.EndHorizontal();

        // 绘制日志列表
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // 如果折叠，只显示不重复的（简易版，这里暂不实现复杂折叠，主要看最新的）
        for (int i = 0; i < logs.Count; i++)
        {
            var log = logs[i];
            GUI.contentColor = logTypeColors[log.type];
            GUILayout.Label(log.message);
            if (log.type == LogType.Exception || log.type == LogType.Error)
            {
                // 报错显示堆栈
                GUILayout.Label(log.stackTrace);
            }
        }
        GUI.contentColor = Color.white;

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}