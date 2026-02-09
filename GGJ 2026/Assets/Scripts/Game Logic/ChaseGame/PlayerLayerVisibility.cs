using UnityEngine;
using Unity.Netcode;
using LayerMap;

/// <summary>
/// 玩家只在「与本地玩家同一图层」时显示。不修改 gameObject.layer，避免与场景（Default/Ground）失去碰撞导致穿地、穿墙。
/// 挂到玩家预制体上。追逐者 E 只抓同图层求生者由 ChaserShockwaveAbility 在逻辑里过滤，不依赖 Unity Layer。
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerLayerVisibility : NetworkBehaviour
{
    [Header("显隐控制")]
    [Tooltip("控制显隐的根节点（空则用第一个子物体或自身）；不同图层时隐藏。勿用带 NetworkObject 的根做 visualRoot。")]
    [SerializeField] private GameObject visualRoot;

    private void Update()
    {
        if (!IsSpawned) return;
        if (LayerMapManager.Instance == null) return;

        var myClient = LayerMapManager.Instance.GetClient(OwnerClientId);
        MapLayer myLayer = myClient != null ? myClient.Layer.Value : MapLayer.Main;
        var localClient = LayerMapManager.Instance.Client;
        MapLayer localLayer = localClient != null ? localClient.Layer.Value : MapLayer.Main;

        // 仅当双方都有 LayerMapClient 且图层一致时才显示；拿不到对方图层或图层不同则隐藏
        bool sameLayer = (myClient != null && localClient != null) && (myLayer == localLayer);

        if (IsOwner)
        {
            SetVisualVisible(true);
        }
        else
        {
            SetVisualVisible(sameLayer);
        }
    }

    private void SetVisualVisible(bool visible)
    {
        if (visualRoot != null)
        {
            if (visualRoot.activeSelf != visible)
                visualRoot.SetActive(visible);
            return;
        }
        // 未指定 visualRoot 时隐藏/显示全部视觉：所有子物体 + 自身 SpriteRenderer，避免只关第一个子物体导致其他仍可见
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = visible;
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;
            if (child.activeSelf != visible)
                child.SetActive(visible);
        }
    }
}
