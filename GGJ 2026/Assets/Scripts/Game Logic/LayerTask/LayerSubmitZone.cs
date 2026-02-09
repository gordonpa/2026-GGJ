using Network;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 挂在“特定图层的特定提交点”上。携带对应物品的阵营玩家进入时提交成功，并输出完成标志（LayerTaskManager 中 flag 为 true）。
/// 提交成功时会给该玩家的 LayerMapClient 加分，UIMain 排行榜会据此刷新。
/// </summary>
public class LayerSubmitZone : NetworkBehaviour
{
    [Header("提交点配置")]
    [Tooltip("该提交点所在图层 ID")]
    [SerializeField] private int layerId;
    [Tooltip("需要提交的任务物品 ID，与 LayerCollectible 的 ItemId 对应")]
    [SerializeField] private int requiredItemId;
    [Tooltip("允许提交的阵营 ID")]
    [SerializeField] private int factionId;
    [Tooltip("提交成功时给该玩家加的分（写入 LayerMapClient.Score，UIMain 排行榜显示）")]
    [SerializeField] private int scorePerSubmit = 10;

    private Collider2D col;



    // LayerSubmitZone.cs  
    public bool CanSubmitBy(CarriedItemHolder holder, FactionMember faction, int playerLayerId)
    {
        if (holder == null || faction == null) return false;

        // 方案B：只要有物品就行，不检查具体ID
        if (!holder.HasItem) return false;

        // 原代码：if (holder.CarriedItemId != requiredItemId) return false;
        // 已移除：不再限定必须是某个特定物品

        if (faction.FactionId != factionId) return false;
        if (playerLayerId != layerId) return false;  // 确保在主层（layerId=0）

        return true;
    }

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        // 强制：所有提交点都设在主层（0）
        layerId = 0;

        var rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitServerRpc(ulong playerClientId)
    {

        if (!IsServer)
        {
            return;
        }

        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.ConnectedClients.TryGetValue(playerClientId, out var client))
        {
            Debug.LogWarning($"[SubmitServerRpc] 找不到玩家 clientId={playerClientId}");
            return;
        }

        var playerObject = client.PlayerObject;
        if (playerObject == null)
        {
            Debug.LogWarning($"[SubmitServerRpc] 玩家 PlayerObject 为 null");
            return;
        }

        var holder = playerObject.GetComponent<CarriedItemHolder>();
        var faction = playerObject.GetComponent<FactionMember>();

        Debug.Log($"[SubmitServerRpc] 获取组件: holder={holder != null}, faction={faction != null}, HasItem={holder?.HasItem}, ItemId={holder?.CarriedItemId}");

        if (holder == null || faction == null)
        {
            Debug.LogWarning($"[SubmitServerRpc] holder 或 faction 为 null");
            return;
        }

        // 方案B：只要有物品就行（不检查具体ID）
        if (!holder.HasItem)
        {
            Debug.LogWarning($"[SubmitServerRpc] 玩家没有携带物品");
            return;
        }

        // 保存提交的物品ID用于日志
        int submittedItemId = holder.CarriedItemId;

        holder.ClearCarriedItemServer();

        var carriedVisual = playerObject.GetComponent<CarriedItemVisual>();
        if (carriedVisual == null)
        {
            Debug.LogWarning($"[SubmitServerRpc] 玩家 {playerClientId} 没有 CarriedItemVisual");
        }
        else
        {
            Debug.Log($"[SubmitServerRpc] 调用视觉隐藏");
            carriedVisual.RefreshCarriedVisualClientRpc(CarriedItemVisual.HideItemId);
            carriedVisual.ApplyCarriedVisual(CarriedItemVisual.HideItemId);
        }

        var taskManager = LayerTaskManager.Instance;
        if (taskManager != null)
        {
            taskManager.CompleteFactionTask(faction.FactionId);
        }

        // 给该玩家加分，UIMain 排行榜从 LayerMapClient.Score 读取并刷新
        if (scorePerSubmit > 0 && NetworkManagerEx.Instance != null && NetworkManagerEx.Instance.Server != null)
        {
            NetworkManagerEx.Instance.Server.AddScoreServerRpc(playerClientId, scorePerSubmit);
        }

        // 阵营总分（供 VictoryConditionManager 胜利判定：SurvivorScoreProvider.GetScore() >= winScoreThreshold）
        if (scorePerSubmit > 0 && faction.FactionId == 0)
        {
            SurvivorScoreProvider.AddScore(scorePerSubmit);
            int currentScore = SurvivorScoreProvider.GetScore();
            var vcm = FindObjectOfType<VictoryConditionManager>();
            int winScoreThreshold = vcm != null ? vcm.winScoreThreshold : 0;
            bool reached = currentScore >= winScoreThreshold;
            Debug.Log($"[LayerSubmitZone] 阵营总分 +{scorePerSubmit} = {currentScore} | 当前分数={currentScore}, winScoreThreshold={winScoreThreshold}, {(reached ? "已达标" : "未达标")}");
        }
    }

    // 归还改为按 E 圆形范围判定，由 LayerInteractInput 调用 SubmitServerRpc，不再使用碰撞触发。
}
