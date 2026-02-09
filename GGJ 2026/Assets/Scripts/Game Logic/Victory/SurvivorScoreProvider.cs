/// <summary>
/// 求生者阵营（FactionId=0）的累加积分，供外部接口写入（如提交道具时加分的逻辑）。
/// 胜利判定会读取 GetScore() 与获胜临界值比较。
/// </summary>
public static class SurvivorScoreProvider
{
    private static int _score;

    /// <summary>当前求生者阵营总积分。</summary>
    public static int GetScore() => _score;

    /// <summary>设置积分（如新一局重置为 0）。</summary>
    public static void SetScore(int value) => _score = value;

    /// <summary>增加积分，由提交道具等逻辑调用。</summary>
    public static void AddScore(int value) => _score += value;
}
