using GameJam.LayerTask;
using LayerMap;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ItemSpawner : NetworkBehaviour
{
    [Header("配置")]
    public SpawnProfile profile;
    public Transform[] spawnPoints;

    [Header("调试")]
    public float spawnRadius = 1.5f;

    private System.Random rng = new System.Random();
    private static bool hasSpawned = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        if (hasSpawned) return;
        if (profile == null || spawnPoints.Length == 0) return;

        hasSpawned = true;
        StartCoroutine(SpawnAll());
    }

    private IEnumerator SpawnAll()
    {
        var layerTasks = profile.GetLayerTasks();

        foreach (var kv in layerTasks)
        {
            MapLayer layer = kv.Key;
            List<ItemDefinition> items = kv.Value;

            foreach (var item in items)
            {
                if (item.prefab == null) continue;

                Transform point = spawnPoints[rng.Next(spawnPoints.Length)];
                Vector2 offset = Random.insideUnitCircle * spawnRadius;
                Vector3 pos = point.position + (Vector3)offset;

                var obj = Instantiate(item.prefab, pos, Quaternion.identity);

                // 🔥 修正点：改回使用 Config 进行转换，确保和玩家 ID 一致！
                // 玩家：Layer2 -> MapLayerToLayerId -> 2
                // 物品：Layer2 -> MapLayerToLayerId -> 2
                // 2 == 2，匹配成功！
                int layerIdConverted = GameLayerSkillConfig.MapLayerToLayerId(layer);

                // 调试日志
                Debug.Log($"[ItemSpawner] 生成物品 {item.itemId} | Enum:{layer} -> Int:{layerIdConverted}");

                // 设置拾取逻辑数据
                var col = obj.GetComponent<LayerCollectible>();
                if (col != null)
                {
                    col.Setup(item.itemId, layerIdConverted, 0);
                }

                // 设置可见性数据
                var visibility = obj.GetComponent<ItemLayerVisibility>();
                if (visibility != null)
                {
                    visibility.initialLayer = layer; // 这里保持用 Enum，因为 Visibility 脚本是用 Enum 比较的
                }

                // 添加标记
                var sign = obj.GetComponent<LayerSign>();
                if (sign == null) sign = obj.AddComponent<LayerSign>();
                sign.Layer = layer;
                sign.Follow = item.canCarryCrossLayer;

                var netObj = obj.GetComponent<NetworkObject>();
                if (netObj != null) netObj.Spawn();

                yield return null;
            }
        }

        yield return new WaitForSeconds(0.5f);
        if (LayerMapManager.Instance != null)
        {
            foreach (var client in LayerMapManager.Instance.AllClient) client?.RefreshLayerStatus();
        }
    }

    public static void ResetForNewGame()
    {
        hasSpawned = false;
    }
}