using LayerMap;
using UnityEngine;

/// <summary>
/// 追逐/求生技能与图层配置。主图层 + 三个特殊图层，求生者面具 0/1/2 对应 Layer1/2/3。
/// </summary>
public static class GameLayerSkillConfig
{
    /// <summary>面具索引 0/1/2 对应的特殊图层。</summary>
    public static MapLayer MaskIndexToLayer(int maskIndex)
    {
        switch (maskIndex)
        {
            case 0: return MapLayer.Layer1;
            case 1: return MapLayer.Layer2;
            case 2: return MapLayer.Layer3;
            default: return MapLayer.Main;
        }
    }

    /// <summary>MapLayer 转 LayerTaskManager 用的 layerId（0=Main, 1=Layer1, 2=Layer2, 3=Layer3）。切换图层时同步到 LayerTaskManager 即可，无需改 LayerCollectible/LayerSubmitZone 的 layerId 逻辑。</summary>
    public static int MapLayerToLayerId(MapLayer layer)
    {
        if (layer == MapLayer.None) return 0;
        int v = (int)layer;
        return v >= 1 && v <= 4 ? v - 1 : 0;
    }

    /// <summary>追逐者 E 冲击波 CD（秒）</summary>
    public const float ChaserShockwaveCd = 5f;
    /// <summary>追逐者 J 切换图层 CD（秒）</summary>
    public const float ChaserLayerMoveCd = 5f;
    /// <summary>追逐者 I 大招 CD（秒）</summary>
    public const float ChaserUltimateCd = 180f;
    /// <summary>大招：求生者普通技能禁用时长（秒）</summary>
    public const float SurvivorSkillDisableDuration = 60f;

    /// <summary>求生者 J 切换图层 CD（秒）</summary>
    public const float SurvivorLayerMoveCd = 20f;
}
