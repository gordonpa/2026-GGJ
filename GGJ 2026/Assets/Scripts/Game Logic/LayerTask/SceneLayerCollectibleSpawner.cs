using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 场景中直接摆放的、带 NetworkObject 的 LayerCollectible，在 Server 加载场景后统一 Spawn，保证客户端能正确同步与拾取。
/// 挂到场景中任意物体（如 LayerTaskMgr 或空物体）上即可；仅 Server 执行。
/// </summary>
public class SceneLayerCollectibleSpawner : MonoBehaviour
{
    private void Start()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        SpawnSceneLayerCollectibles();
    }

    private void SpawnSceneLayerCollectibles()
    {
        var collectibles = FindObjectsOfType<LayerCollectible>(true);
        int spawned = 0;
        foreach (var col in collectibles)
        {
            if (col == null) continue;
            var no = col.GetComponent<NetworkObject>();
            if (no == null) continue;
            if (no.IsSpawned) continue;

            no.Spawn();
            spawned++;
        }

        if (spawned > 0)
            Debug.Log($"[SceneLayerCollectibleSpawner] 已 Spawn 场景中 {spawned} 个 LayerCollectible（带 NetworkObject 且未 Spawn 的）");
    }
}
