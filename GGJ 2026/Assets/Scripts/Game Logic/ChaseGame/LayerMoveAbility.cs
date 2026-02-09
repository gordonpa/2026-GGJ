using Unity.Netcode;
using UnityEngine;
using LayerMap;

/// <summary>
/// 追逐者/求生者 J 键：切换到选定图层。追逐者可选 4 个图层，求生者可选主图层+面具图层（+携带的死亡面具图层），CD 可配置。
/// 需场景存在 LayerMapManager 且已为玩家生成 LayerMapClient（如 ReqGenClient）。
/// </summary>
[RequireComponent(typeof(FactionMember))]
public class LayerMoveAbility : NetworkBehaviour
{
    [Header("CD（秒）")]
    [SerializeField] private float chaserCd = 5f;
    [SerializeField] private float survivorCd = 20f;

    private NetworkVariable<double> nextUseTime = new NetworkVariable<double>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private FactionMember faction;
    private SurvivorState survivorState;

    public double NextUseTime => nextUseTime.Value;
    public float ChaserCd => chaserCd;
    public float SurvivorCd => survivorCd;

    public bool IsOnCooldown()
    {
        var nm = NetworkManager.Singleton;
        return nm != null && nm.ServerTime.Time < nextUseTime.Value;
    }

    public bool CanUse()
    {
        if (faction == null) return false;
        if (faction.IsChaser)
            return !IsOnCooldown();
        if (faction.IsSurvivor)
        {
            if (survivorState != null && survivorState.IsSkillDisabled()) return false;
            return !IsOnCooldown();
        }
        return false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        faction = GetComponent<FactionMember>();
        survivorState = GetComponent<SurvivorState>();
    }

    /// <summary>请求切换到目标图层（由 UI 调用），服务器执行并进入 CD。</summary>
    public void RequestMoveToLayer(MapLayer targetLayer)
    {
        if (!CanUse()) return;
        RequestMoveToLayerServerRpc(targetLayer);
    }

    [ServerRpc]
    private void RequestMoveToLayerServerRpc(MapLayer targetLayer, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;
        if (LayerMapManager.Instance == null) return;
        if (!LayerMapManager.Instance.TryGetClient(clientId, out var client)) return;

        float cd = faction != null && faction.IsChaser ? chaserCd : survivorCd;
        nextUseTime.Value = nm.ServerTime.Time + cd;
        client.GotoLayer(targetLayer);
    }
}
