/// <summary>
/// 联机大厅常量：阵营与面具 ID。求生者 0/1/2，追逐者 3。
/// </summary>
public static class LobbyConstants
{
    /// <summary>未选阵营（大厅中尚未拾取面具）</summary>
    public const int NoFaction = -1;
    /// <summary>求生者阵营（三张正常面具）</summary>
    public const int FactionSurvivor = 0;
    /// <summary>追逐者阵营（特殊面具）</summary>
    public const int FactionChaser = 1;

    /// <summary>面具索引：0/1/2 求生者三种颜色，3 追逐者。</summary>
    public const int MaskIndexSurvivor0 = 0;
    public const int MaskIndexSurvivor1 = 1;
    public const int MaskIndexSurvivor2 = 2;
    public const int MaskIndexChaser = 3;

    public static bool IsSurvivor(int factionId) => factionId == FactionSurvivor;
    public static bool IsChaser(int factionId) => factionId == FactionChaser;
}
