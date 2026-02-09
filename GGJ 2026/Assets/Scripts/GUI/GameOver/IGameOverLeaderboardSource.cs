using System.Collections.Generic;

/// <summary>
/// 游戏结束时排行榜数据来源（由外部实现，如积分统计）。GameManager 结束游戏时会调用并同步给客户端。
/// </summary>
public interface IGameOverLeaderboardSource
{
    /// <summary>当前排行榜：显示名、积分，按名次从高到低。</summary>
    void GetEntries(List<(string displayName, int score)> outEntries);
}
