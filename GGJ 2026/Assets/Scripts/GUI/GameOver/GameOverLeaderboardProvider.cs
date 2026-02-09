using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 游戏结束界面用到的排行榜数据，由 ClientRpc 写入；调试时也可直接 SetEntries。
/// </summary>
public static class GameOverLeaderboardProvider
{
    private static readonly List<(string displayName, int score)> _entries = new List<(string displayName, int score)>();

    public static IReadOnlyList<(string displayName, int score)> GetEntries()
    {
        return _entries;
    }

    /// <summary>从 Rpc 或调试设置排行榜（内部用）。</summary>
    public static void SetEntries(IList<(string displayName, int score)> entries)
    {
        _entries.Clear();
        if (entries != null)
            for (int i = 0; i < entries.Count; i++)
                _entries.Add(entries[i]);
    }

    /// <summary>调试用：直接设置排行榜。</summary>
    public static void SetEntriesFromArrays(string[] names, int[] scores)
    {
        _entries.Clear();
        if (names == null || scores == null) return;
        int n = Mathf.Min(names.Length, scores.Length);
        for (int i = 0; i < n; i++)
            _entries.Add((names[i], scores[i]));
    }

    /// <summary>从 Rpc 编码字符串解析并设置（格式：每行 "displayName\tscore"，行间 \n）。</summary>
    public static void SetEntriesFromEncoded(string encoded)
    {
        _entries.Clear();
        if (string.IsNullOrEmpty(encoded)) return;
        foreach (var line in encoded.Split('\n'))
        {
            var parts = line.Trim().Split('\t');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int score))
                _entries.Add((parts[0], score));
        }
    }

    /// <summary>将当前列表编码为字符串（供 Rpc 发送）。</summary>
    public static string EncodeEntries(IList<(string displayName, int score)> entries)
    {
        if (entries == null || entries.Count == 0) return "";
        var sb = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            if (i > 0) sb.Append('\n');
            sb.Append(entries[i].displayName?.Replace("\t", " ").Replace("\n", " ") ?? "");
            sb.Append('\t');
            sb.Append(entries[i].score);
        }
        return sb.ToString();
    }
}
