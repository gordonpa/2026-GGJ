using Unity.Netcode;
using UnityEngine;
using LayerMap;

/// <summary>
/// 死亡求生者掉落的面具，仅求生者可见与可拾取。需挂 LayerSign(Main)、Collider2D、NetworkObject。
/// 追逐者冲击波击杀求生者后由服务器生成在主图层随机位置。
/// </summary>
[RequireComponent(typeof(LayerSign))]
public class DroppedMaskCollectible : NetworkBehaviour
{
    [Header("面具")]
    [Tooltip("面具索引 0/1/2，对应求生者三种")]
    [SerializeField] private int maskIndex;

    private NetworkVariable<int> maskIndexNet = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    /// <summary>拾取后获得的位掩码：死者自己的面具 + 死者曾携带的死亡面具（继承）。bit0/1/2 = 面具0/1/2。</summary>
    private NetworkVariable<int> grantedMaskBits = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    /// <summary>客户端：若 OnNetworkSpawn 时 Client 为 null 未刷新 LayerSign 列表，则延后重试。</summary>
    private bool _pendingRefreshLayerSign;
    /// <summary>客户端：上次可见状态，避免每帧重复 Set。初始 true 以便首帧不在主图层时也会执行一次隐藏。</summary>
    private bool _lastVisible = true;

    public int MaskIndex => maskIndexNet.Value;

    /// <summary>服务器：生成后设置面具索引（冲击波击杀时调用）。</summary>
    public void SetMaskIndexServer(int index)
    {
        if (IsServer && index >= 0 && index <= 2)
            maskIndexNet.Value = index;
    }

    /// <summary>服务器：设置拾取后获得的位掩码（死者面具 + 死者曾携带的面具）。击杀时由 ChaserShockwave 调用。</summary>
    public void SetGrantedMaskBitsServer(int bits)
    {
        if (IsServer)
            grantedMaskBits.Value = bits & 7;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // 不在这里覆盖 maskIndexNet，由 ChaserShockwave 在生成时设置，避免执行顺序导致被预制体默认值覆盖
        var ls = GetComponent<LayerSign>();
        if (ls != null) ls.Layer = MapLayer.Main;
        if (IsClient)
            TryRefreshLayerSignList();
    }

    private void TryRefreshLayerSignList()
    {
        if (LayerMapManager.Instance == null || LayerMapManager.Instance.Client == null)
        {
            _pendingRefreshLayerSign = true;
            return;
        }
        var sync = LayerMapManager.Instance.Client.SyncControl;
        if (sync != null)
        {
            sync.RefreshLayerSignList();
            LayerMapManager.Instance.Client.RefreshLayerStatus();
        }
        _pendingRefreshLayerSign = false;
    }

    private void Update()
    {
        if (!IsSpawned) return;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsClient) return;
        // 客户端：若生成时 Client 尚未就绪，延后刷新 LayerSign 列表
        if (_pendingRefreshLayerSign)
            TryRefreshLayerSignList();
        var po = nm.LocalClient?.PlayerObject;
        if (po == null) return;
        var faction = po.GetComponent<FactionMember>();
        bool visible;
        if (faction != null && faction.IsChaser)
            visible = false;
        else
            visible = LayerMapManager.Instance?.Client != null && LayerMapManager.Instance.Client.Layer.Value == MapLayer.Main;
        // 不 SetActive 根物体，否则切回主图层时 Update 不再执行、无法再次显示。只开关渲染与碰撞。
        if (visible != _lastVisible)
        {
            _lastVisible = visible;
            SetVisibleAndInteractable(visible);
        }
    }

    private void SetVisibleAndInteractable(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = visible;
        foreach (var c in GetComponentsInChildren<Collider2D>(true))
            c.enabled = visible;
    }

    /// <summary>服务器：由 LayerInteractInput 按“距离最近的主图层求生者”判定后调用，执行拾取。不依赖 SenderClientId，避免同机多窗口时只有 Host 收到按键导致拾取者恒为 0。</summary>
    public void TryExecutePickupForClient(ulong survivorClientId, float interactRadius)
    {
        if (!IsServer) return;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.ConnectedClients.TryGetValue(survivorClientId, out var client) || client.PlayerObject == null) return;
        float distSq = ((Vector2)client.PlayerObject.transform.position - (Vector2)transform.position).sqrMagnitude;
        if (distSq > interactRadius * interactRadius) return;
        var faction = client.PlayerObject.GetComponent<FactionMember>();
        if (faction == null || !faction.IsSurvivor) return;
        if (LayerMapManager.Instance == null || !LayerMapManager.Instance.TryGetClient(survivorClientId, out var layerClient) || layerClient.Layer.Value != MapLayer.Main)
        {
            GameLogGUI.AddLine($"[面具] 拾取拒绝: survivor={survivorClientId} 不在主图层");
            return;
        }
        int granted = grantedMaskBits.Value & 7;
        if (granted == 0)
        {
            int single = maskIndexNet.Value;
            if (single >= 0 && single <= 2)
                granted = (1 << single) & 7;
        }
        var state = client.PlayerObject.GetComponent<SurvivorState>();
        int countBefore = state != null ? state.GetCarriedDeadMaskIndices().Count : 0;
        if (state != null && granted != 0)
            state.AddCarriedDeadMaskBitsServer(granted);
        GameLogGUI.AddLine($"[面具] 拾取者(按距离) clientId={survivorClientId} grantedBits={granted} 拾取前已携带数={countBefore}");
        GetComponent<NetworkObject>().Despawn(true);
    }
}
