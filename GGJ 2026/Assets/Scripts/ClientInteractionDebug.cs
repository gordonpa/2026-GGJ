using UnityEngine;
using Unity.Netcode;
using LayerMap;

public class ClientInteractionDebug : NetworkBehaviour
{
    [Header("调试设置")]
    public float detectRadius = 3.0f;
    public KeyCode debugKey = KeyCode.E;

    private void Update()
    {
        if (!IsSpawned || !IsLocalPlayer) return;

        if (Input.GetKeyDown(debugKey))
        {
            PerformDebugCheck();
        }
    }

    private void PerformDebugCheck()
    {
        Debug.Log($"<color=cyan>====== [客户端 E键 诊断开始] ======</color>");

        // 🔥 修复点：不再 GetComponent，而是从管理器单例获取
        int myLayerInt = 0;
        MapLayer myLayerEnum = MapLayer.Main;

        // 尝试获取本地数据
        if (LayerMapManager.Instance != null && LayerMapManager.Instance.Client != null)
        {
            var client = LayerMapManager.Instance.Client;
            myLayerEnum = client.Layer.Value;
            myLayerInt = GameLayerSkillConfig.MapLayerToLayerId(myLayerEnum);
        }
        else
        {
            Debug.LogError("❌ 无法获取 LayerMapManager.Instance.Client！无法确定玩家层级！");
            return;
        }

        Debug.Log($"🕵️‍♂️ <b>玩家状态:</b>\n" +
                  $"   - Enum: {myLayerEnum}\n" +
                  $"   - Int ID: <color=yellow>{myLayerInt}</color>");

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadius);
        bool foundTarget = false;

        foreach (var hit in hits)
        {
            var item = hit.GetComponent<LayerCollectible>();
            if (item == null) continue;

            foundTarget = true;
            int itemNetId = item.LayerId;

            // 核心判定
            bool idMatch = (myLayerInt == itemNetId);
            string matchResult = idMatch ? "<color=green>匹配成功</color>" : $"<color=red>匹配失败 ({myLayerInt}!={itemNetId})</color>";

            Debug.Log($"📦 <b>物品: {item.name}</b>\n" +
                      $"   - LayerId: <color=yellow>{itemNetId}</color>\n" +
                      $"   - 判定: {matchResult}");
        }

        if (!foundTarget) Debug.LogWarning("🤷‍♂️ 3米内无物品");
        Debug.Log($"<color=cyan>====== [诊断结束] ======</color>");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}