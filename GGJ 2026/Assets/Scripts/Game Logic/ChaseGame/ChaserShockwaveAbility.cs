using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using LayerMap;

/// <summary>
/// 追逐者 E 键：圆形冲击波扫描抓捕范围内求生者（仅同图层），被抓住则死亡、面具掉落在主图层随机位置。CD 可配置。
/// </summary>
[RequireComponent(typeof(FactionMember))]
public class ChaserShockwaveAbility : NetworkBehaviour
{
    [Header("冲击波")]
    [Tooltip("冲击波半径（世界单位）")]
    [SerializeField] private float shockwaveRadius = 4f;
    [Tooltip("CD（秒）")]
    [SerializeField] private float cooldownSeconds = 5f;

    [Header("掉落面具预制体")]
    [Tooltip("Resources 下预制体名，需带 NetworkObject 与 DroppedMaskCollectible")]
    [SerializeField] private string droppedMaskPrefabName = "DroppedMaskCollectible";

    [Header("冲击波范围可视化（仅本地追逐者可见）")]
    [SerializeField] private bool showShockwaveRange = true;
    [SerializeField] private Color shockwaveRangeColor = new Color(1f, 0.3f, 0.2f, 0.4f);
    [SerializeField] private int circleSegments = 48;

    private LineRenderer shockwaveLine;
    private NetworkVariable<double> nextUseTime = new NetworkVariable<double>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private FactionMember faction;

    public float CooldownSeconds => cooldownSeconds;
    public float ShockwaveRadius => shockwaveRadius;
    public double NextUseTime => nextUseTime.Value;
    public bool IsOnCooldown()
    {
        var nm = NetworkManager.Singleton;
        return nm != null && nm.ServerTime.Time < nextUseTime.Value;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        faction = GetComponent<FactionMember>();
    }

    private void LateUpdate()
    {
        if (shockwaveLine == null && IsOwner && faction != null && faction.IsChaser && showShockwaveRange)
            BuildShockwaveRangeVisual();
    }

    private void BuildShockwaveRangeVisual()
    {
        var go = new GameObject("ShockwaveRangeCircle");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        shockwaveLine = go.AddComponent<LineRenderer>();
        shockwaveLine.useWorldSpace = false;
        shockwaveLine.loop = true;
        shockwaveLine.positionCount = circleSegments + 1;
        shockwaveLine.startWidth = 0.06f;
        shockwaveLine.endWidth = 0.06f;
        shockwaveLine.material = new Material(Shader.Find("Sprites/Default"));
        shockwaveLine.startColor = shockwaveRangeColor;
        shockwaveLine.endColor = shockwaveRangeColor;
        shockwaveLine.sortingOrder = 99;
        shockwaveLine.sortingLayerName = "Default";
        for (int i = 0; i <= circleSegments; i++)
        {
            float t = (float)i / circleSegments * Mathf.PI * 2f;
            float x = shockwaveRadius * Mathf.Cos(t);
            float y = shockwaveRadius * Mathf.Sin(t);
            shockwaveLine.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    public override void OnDestroy()
    {
        if (shockwaveLine != null && shockwaveLine.gameObject != null)
        {
            if (Application.isPlaying)
                Object.Destroy(shockwaveLine.gameObject);
            else
                Object.DestroyImmediate(shockwaveLine.gameObject);
        }
        base.OnDestroy();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (!faction.IsChaser) return;
        if (Input.GetKeyDown(KeyCode.E))
        {
            UIMgr.Get<UIMain>().MainSkil?.UseSkill();
            TryShockwaveServerRpc();
        }
    }

    [ServerRpc]
    private void TryShockwaveServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.ConnectedClients.TryGetValue(clientId, out var chaserClient) || chaserClient.PlayerObject == null)
            return;
        var chaserFaction = chaserClient.PlayerObject.GetComponent<FactionMember>();
        if (chaserFaction == null || !chaserFaction.IsChaser) return;
        if (nm.ServerTime.Time < nextUseTime.Value) return;

        nextUseTime.Value = nm.ServerTime.Time + cooldownSeconds;
        Vector2 origin = chaserClient.PlayerObject.transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, shockwaveRadius);

        MapLayer chaserLayer = MapLayer.Main;
        if (LayerMapManager.Instance != null && LayerMapManager.Instance.TryGetClient(clientId, out var chaserLayerClient))
            chaserLayer = chaserLayerClient.Layer.Value;

        var prefab = Resources.Load<GameObject>(droppedMaskPrefabName);
        if (prefab == null) prefab = Resources.Load<NetworkObject>(droppedMaskPrefabName)?.gameObject;
        bool hasSpawner = GameManager.Instance != null;

        if (prefab == null)
        {
            var msg = $"掉落面具预制体未找到: Resources.Load(\"{droppedMaskPrefabName}\") 为空";
            Debug.LogWarning($"[ChaserShockwave] {msg}");
            GameLogGUI.AddWarning(msg);
        }
        else if (prefab.GetComponent<NetworkObject>() == null)
        {
            var msg = "掉落面具预制体上无 NetworkObject，无法 Spawn";
            Debug.LogWarning("[ChaserShockwave] " + msg);
            GameLogGUI.AddWarning(msg);
        }

        foreach (var col in hits)
        {
            var no = col.GetComponent<NetworkObject>();
            if (no == null || !no.IsPlayerObject) continue;
            var targetFaction = no.GetComponent<FactionMember>();
            if (targetFaction == null || !targetFaction.IsSurvivor) continue;
            var targetState = no.GetComponent<SurvivorState>();
            if (targetState != null && targetState.IsDead) continue;

            // 仅抓捕与追逐者同一图层的求生者
            if (LayerMapManager.Instance != null && LayerMapManager.Instance.TryGetClient(no.OwnerClientId, out var targetLayerClient))
            {
                if (targetLayerClient.Layer.Value != chaserLayer)
                    continue;
            }

            int victimMaskId = targetFaction.MaskId;
            if (victimMaskId < 0 || victimMaskId > 2) victimMaskId = 0;
            int victimCarriedBits = targetState != null ? (targetState.CarriedDeadMaskBitsValue & 7) : 0;
            if (targetState != null)
                targetState.SetDeadServer(true);
            int grantedBits = (1 << victimMaskId) | victimCarriedBits;
            if (prefab != null)
            {
                var netObj = prefab.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    Vector3 dropPos = hasSpawner ? GameManager.Instance.GetRandomPositionInBounds() : (Vector3)(origin + Random.insideUnitCircle * 3f);
                    var go = Object.Instantiate(prefab, dropPos, Quaternion.identity);
                    var dropped = go.GetComponent<DroppedMaskCollectible>();
                    netObj = go.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Spawn();
                        if (dropped != null)
                        {
                            dropped.SetMaskIndexServer(victimMaskId);
                            dropped.SetGrantedMaskBitsServer(grantedBits);
                        }
                        GameLogGUI.AddLine($"掉落面具 victimMaskId={victimMaskId} victimCarriedBits={victimCarriedBits} grantedBits={grantedBits}");
                    }
                }
            }
        }
    }
}
