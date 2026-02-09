using Unity.Netcode;
using UnityEngine;
using LayerMap;

/// <summary>
/// 挂在玩家预制体上。按 E 在圆形范围内判定拾取/归还。
/// 【修复版】不再依赖 Physics2D，改用 FindObjectsOfType，解决 Host 视角禁用碰撞体导致检测不到的问题。
/// </summary>
[RequireComponent(typeof(CarriedItemHolder))]
[RequireComponent(typeof(FactionMember))]
public class LayerInteractInput : NetworkBehaviour
{
    [Header("交互")]
    [Tooltip("交互键")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [Tooltip("拾取/归还的圆形判定半径（世界单位）")]
    [SerializeField] private float interactRadius = 2.5f;

    [Header("范围可视化")]
    [Tooltip("是否显示交互范围圆圈（仅本地玩家）")]
    [SerializeField] private bool showRangeCircle = true;
    [SerializeField] private Color rangeCircleColor = new Color(1f, 1f, 0.3f, 0.35f);
    [SerializeField] private int circleSegments = 48;

    private LineRenderer rangeLine;
    private CarriedItemHolder holder;
    private FactionMember faction;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        holder = GetComponent<CarriedItemHolder>();
        faction = GetComponent<FactionMember>();
        if (IsOwner && showRangeCircle)
            BuildRangeCircle();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(interactKey))
        {
            TryInteractServerRpc(interactRadius);
        }
    }

    [ServerRpc]
    private void TryInteractServerRpc(float interactRadiusParam, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null) return;

        Transform playerTransform = client.PlayerObject.transform;
        Vector2 origin = playerTransform.position;
        var holderComp = client.PlayerObject.GetComponent<CarriedItemHolder>();
        var factionComp = client.PlayerObject.GetComponent<FactionMember>();
        if (holderComp == null || factionComp == null) return;

        // 1. 获取玩家准确的图层 ID
        int playerLayerId = 0;
        if (LayerMapManager.Instance != null && LayerMapManager.Instance.TryGetClient(clientId, out var layerClient))
        {
            playerLayerId = GameLayerSkillConfig.MapLayerToLayerId(layerClient.Layer.Value);
        }

        // 物理检测只用于那些不会隐藏碰撞体的物体（如 Lobby 里的台座）
        Collider2D[] physicsHits = Physics2D.OverlapCircleAll(origin, interactRadiusParam);

        // --- 逻辑 A: 大厅选面具 ---
        bool isLobby = LobbyManager.Instance != null && LobbyManager.Instance.IsLobbyPhase;
        if (isLobby && !factionComp.HasMask)
        {
            LobbyMaskPedestal bestPedestal = null;
            float bestDistSq = float.MaxValue;
            foreach (var col in physicsHits)
            {
                var pedestal = col.GetComponent<LobbyMaskPedestal>();
                if (pedestal == null || !pedestal.CanBePickedBy(factionComp)) continue;
                float distSq = ((Vector2)col.transform.position - origin).sqrMagnitude;
                if (distSq < bestDistSq) { bestDistSq = distSq; bestPedestal = pedestal; }
            }
            if (bestPedestal != null) { bestPedestal.PickUpServerRpc(clientId); return; }
        }

        if (factionComp.IsChaser) return;

        // --- 逻辑 B: 求生者捡掉落面具 ---
        // (保持原逻辑，这里本来就是用 FindObjectsOfType，所以本来就是好的)
        if (factionComp.IsSurvivor)
        {
            float rSq = interactRadiusParam * interactRadiusParam;
            DroppedMaskCollectible bestDropped = null;
            float bestDistSq = float.MaxValue;
            var allDropped = Object.FindObjectsOfType<DroppedMaskCollectible>();
            foreach (var dropped in allDropped)
            {
                if (dropped == null || !dropped.IsSpawned) continue;
                float dSq = ((Vector2)dropped.transform.position - origin).sqrMagnitude;
                if (dSq <= rSq && dSq < bestDistSq) { bestDistSq = dSq; bestDropped = dropped; }
            }
            if (bestDropped != null) { bestDropped.TryExecutePickupForClient(clientId, interactRadiusParam); return; }
        }

        // --- 逻辑 C: 归还物品 (Submit) ---
        bool trySubmitFirst = holderComp.HasItem;
        if (trySubmitFirst)
        {
            LayerSubmitZone bestSubmit = null;
            float bestDistSq = float.MaxValue;

            // 🔥 修复：改用 FindObjectsOfType 扫描所有提交点，防止 Host 禁用碰撞体导致找不到
            var allZones = Object.FindObjectsOfType<LayerSubmitZone>();

            foreach (var zone in allZones)
            {
                if (zone == null || !zone.IsSpawned) continue; // 确保物体存在

                float distSq = ((Vector2)zone.transform.position - origin).sqrMagnitude;
                if (distSq > interactRadiusParam * interactRadiusParam) continue; // 超出范围

                // 传入玩家图层判定
                if (!zone.CanSubmitBy(holderComp, factionComp, playerLayerId)) continue;

                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestSubmit = zone;
                }
            }

            if (bestSubmit != null)
            {
                bestSubmit.SubmitServerRpc(clientId);
                return;
            }
        }

        // --- 逻辑 D: 拾取物品 (Pickup) ---
        LayerCollectible bestCollectible = null;
        float bestCollectibleDistSq = float.MaxValue;

        // 🔥🔥 核心修复：改用 FindObjectsOfType 扫描所有 LayerCollectible 🔥🔥
        // 这样即使 ItemLayerVisibility 把碰撞体关了，我们依然能找到它并计算距离！
        var allCollectibles = Object.FindObjectsOfType<LayerCollectible>();

        foreach (var collectible in allCollectibles)
        {
            // 基础过滤
            if (collectible == null || !collectible.IsSpawned || collectible.IsPickedUp) continue;

            // 距离检测
            float distSq = ((Vector2)collectible.transform.position - origin).sqrMagnitude;
            if (distSq > interactRadiusParam * interactRadiusParam) continue;

            // 逻辑判定 (图层匹配)
            if (!collectible.CanBePickedBy(holderComp, factionComp, playerLayerId)) continue;

            if (distSq < bestCollectibleDistSq)
            {
                bestCollectibleDistSq = distSq;
                bestCollectible = collectible;
            }
        }

        if (bestCollectible != null)
        {
            bestCollectible.PickUpServerRpc(clientId);
        }
    }

    // (Visual代码保持不变)
    private void BuildRangeCircle()
    {
        var go = new GameObject("InteractRangeCircle");
        go.transform.SetParent(transform, false);
        rangeLine = go.AddComponent<LineRenderer>();
        rangeLine.useWorldSpace = false;
        rangeLine.loop = true;
        rangeLine.positionCount = circleSegments + 1;
        rangeLine.startWidth = 0.08f;
        rangeLine.endWidth = 0.08f;
        rangeLine.material = new Material(Shader.Find("Sprites/Default"));
        rangeLine.startColor = rangeCircleColor;
        rangeLine.endColor = rangeCircleColor;
        rangeLine.sortingOrder = 100;
        for (int i = 0; i <= circleSegments; i++)
        {
            float t = (float)i / circleSegments * Mathf.PI * 2f;
            rangeLine.SetPosition(i, new Vector3(interactRadius * Mathf.Cos(t), interactRadius * Mathf.Sin(t), 0f));
        }
    }

    public override void OnDestroy()
    {
        if (rangeLine != null && rangeLine.gameObject != null)
        {
            if (Application.isPlaying) Destroy(rangeLine.gameObject);
            else DestroyImmediate(rangeLine.gameObject);
        }
        base.OnDestroy();
    }
}