using Unity.Netcode;
using UnityEngine;
using LayerMap;

/// <summary>
/// 追逐者 I 键大招：将全部求生者召唤到追逐者所在图层，并禁用求生者普通技能（J）一段时间。CD 可配置。
/// </summary>
[RequireComponent(typeof(FactionMember))]
public class ChaserUltimateAbility : NetworkBehaviour
{
    [Header("大招")]
    [Tooltip("CD（秒）")]
    [SerializeField] private float cooldownSeconds = 180f;
    [Tooltip("求生者 J 技能禁用时长（秒）")]
    [SerializeField] private float survivorDisableDuration = 60f;

    private NetworkVariable<double> nextUseTime = new NetworkVariable<double>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private FactionMember faction;

    public float CooldownSeconds => cooldownSeconds;
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

    private void Update()
    {
        if (!IsOwner) return;
        if (!faction.IsChaser) return;
        if (Input.GetKeyDown(KeyCode.I))
        {
            UIMgr.Get<UIMain>().SubSkill?.UseSkill();
            TryUltimateServerRpc();
        }
    }

    [ServerRpc]
    private void TryUltimateServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;
        if (LayerMapManager.Instance == null) return;
        if (!nm.ConnectedClients.TryGetValue(clientId, out var chaserClient) || chaserClient.PlayerObject == null) return;
        var chaserFaction = chaserClient.PlayerObject.GetComponent<FactionMember>();
        if (chaserFaction == null || !chaserFaction.IsChaser) return;
        if (nm.ServerTime.Time < nextUseTime.Value) return;

        nextUseTime.Value = nm.ServerTime.Time + cooldownSeconds;
        if (!LayerMapManager.Instance.TryGetClient(clientId, out var chaserLayerClient)) return;
        var targetLayer = chaserLayerClient.Layer.Value;
        double disableUntil = nm.ServerTime.Time + survivorDisableDuration;

        foreach (var kv in nm.ConnectedClients)
        {
            var po = kv.Value.PlayerObject;
            if (po == null) continue;
            var f = po.GetComponent<FactionMember>();
            if (f == null || !f.IsSurvivor) continue;
            if (LayerMapManager.Instance.TryGetClient(kv.Key, out var survivorLayerClient))
                survivorLayerClient.GotoLayer(targetLayer);
            var state = po.GetComponent<SurvivorState>();
            if (state != null)
                state.SetSkillDisabledUntilServer(disableUntil);
        }
    }
}
