using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 图层任务管理器：维护当前图层（由外部设置）、阵营任务完成状态，对外提供完成标志接口。
/// 当前图层由外部在切换图层时调用 SetCurrentLayer 写入。
/// </summary>
public class LayerTaskManager : MonoBehaviour
{
    public static LayerTaskManager Instance { get; private set; }

    [Header("当前图层（也可由外部通过 SetCurrentLayer 设置）")]
    [SerializeField] private int currentLayerId;

    private readonly HashSet<int> completedFactionIds = new HashSet<int>();

    /// <summary>当前显示的图层 ID，由外部切换图层时设置。</summary>
    public static int CurrentLayerId => Instance != null ? Instance.currentLayerId : 0;

    /// <summary>外部设置当前图层（切换图层时调用）。</summary>
    public static void SetCurrentLayer(int layerId)
    {
        if (Instance != null)
        {
            int old = Instance.currentLayerId;
            Instance.currentLayerId = layerId;
            if (old != layerId)
                GameLogGUI.AddLine($"[LayerTask] layerId {old}→{layerId}");
        }
    }

    /// <summary>对外接口：某阵营任务是否已完成（flag 为 true）。</summary>
    public static bool IsFactionTaskCompleted(int factionId)
    {
        return Instance != null && Instance.completedFactionIds.Contains(factionId);
    }

    /// <summary>任务完成时触发，参数为阵营 ID。</summary>
    public static event Action<int> OnFactionTaskCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>内部调用：标记某阵营任务完成，并触发事件。</summary>
    public void CompleteFactionTask(int factionId)
    {
        if (completedFactionIds.Contains(factionId)) return;
        completedFactionIds.Add(factionId);
        OnFactionTaskCompleted?.Invoke(factionId);
    }

    /// <summary>获取当前图层（实例方法，供同场景组件使用）。</summary>
    public int GetCurrentLayer() => currentLayerId;

    // 在 LayerTaskManager.cs 中添加：

    /// <summary>持久化记录已拾取的物品ID（跨场景保持）</summary>
    private static HashSet<string> pickedUpItemIds = new HashSet<string>();

    /// <summary>标记物品已被拾取（持久化）</summary>
    public static void MarkItemPickedUp(string uniqueItemId)
    {
        if (string.IsNullOrEmpty(uniqueItemId)) return;
        pickedUpItemIds.Add(uniqueItemId);
        Debug.Log($"[LayerTaskManager] 记录已拾取物品: {uniqueItemId}");
    }

    /// <summary>检查物品是否已被拾取</summary>
    public static bool IsItemPickedUp(string uniqueItemId)
    {
        if (string.IsNullOrEmpty(uniqueItemId)) return false;
        return pickedUpItemIds.Contains(uniqueItemId);
    }

    /// <summary>新游戏开始时清空记录（返回大厅时调用）</summary>
    public static void ClearPickedUpItems()
    {
        pickedUpItemIds.Clear();
        Debug.Log("[LayerTaskManager] 清空已拾取物品记录");
    }
}
