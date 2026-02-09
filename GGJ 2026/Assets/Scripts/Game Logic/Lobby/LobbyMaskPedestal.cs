using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 大厅面具台：放在四个台子上，面具先到先得。玩家按 E 范围交互拾取后获得阵营与皮肤。
/// maskIndex: 0/1/2 求生者三种颜色，3 追逐者。
/// </summary>
public class LobbyMaskPedestal : NetworkBehaviour
{
    [Header("面具")]
    [Tooltip("面具索引：0/1/2 求生者，3 追逐者")]
    [SerializeField] private int maskIndex = 0;

    private bool isPickedUp;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    public int MaskIndex => maskIndex;
    public bool IsPickedUp => isPickedUp;
    public int FactionId => maskIndex < LobbyConstants.MaskIndexChaser ? LobbyConstants.FactionSurvivor : LobbyConstants.FactionChaser;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
        }
        else
            col.isTrigger = true;
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isPickedUp = false;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (spriteRenderer != null) spriteRenderer.enabled = visible;
        if (col != null) col.enabled = visible;
    }

    /// <summary>是否可被该玩家拾取（未拾取、玩家尚未选阵营）。</summary>
    public bool CanBePickedBy(FactionMember faction)
    {
        if (isPickedUp || faction == null || faction.HasMask) return false;
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void PickUpServerRpc(ulong playerClientId)
    {
        if (!IsServer || isPickedUp) return;

        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.ConnectedClients.TryGetValue(playerClientId, out var client) || client.PlayerObject == null)
            return;

        var factionComp = client.PlayerObject.GetComponent<FactionMember>();
        if (factionComp == null || factionComp.HasMask) return;

        isPickedUp = true;
        int faction = FactionId;
        factionComp.SetMaskServer(faction, maskIndex);
        SetVisibleClientRpc(false);

        if (LobbyManager.Instance != null)
            LobbyManager.Instance.OnMaskPicked(playerClientId);
    }

    [ClientRpc]
    private void SetVisibleClientRpc(bool visible)
    {
        SetVisible(visible);
    }
}
