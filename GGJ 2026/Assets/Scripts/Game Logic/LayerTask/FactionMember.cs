using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 挂在玩家预制体上，表示该玩家所属阵营与面具。大厅中未拾取面具时为 NoFaction(-1)；拾取后为求生者(0)或追逐者(1)，并带面具 ID(0~3)。
/// </summary>
public class FactionMember : NetworkBehaviour
{
    [Tooltip("初始阵营 ID；大厅场景用 -1（未选），运行时由 LobbyMaskPedestal 设置")]
    [SerializeField] private int initialFactionId = -1;

    private NetworkVariable<int> factionId = new NetworkVariable<int>(LobbyConstants.NoFaction,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    /// <summary>面具索引 0/1/2 求生者三种颜色，3 追逐者；-1 未选。</summary>
    private NetworkVariable<int> maskId = new NetworkVariable<int>(-1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    /// <summary>追逐者开局冻结结束时间（ServerTime），0 表示未冻结。</summary>
    private NetworkVariable<double> chaserFreezeUntil = new NetworkVariable<double>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int FactionId => factionId.Value;
    public int MaskId => maskId.Value;
    public bool HasMask => factionId.Value != LobbyConstants.NoFaction;
    public NetworkVariable<int> FactionIdVariable => factionId;
    public NetworkVariable<int> MaskIdVariable => maskId;
    public bool IsSurvivor => factionId.Value == LobbyConstants.FactionSurvivor;
    public bool IsChaser => factionId.Value == LobbyConstants.FactionChaser;
    public double ChaserFreezeUntil => chaserFreezeUntil.Value;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && initialFactionId != LobbyConstants.NoFaction)
        {
            factionId.Value = initialFactionId;
            if (maskId.Value < 0)
                maskId.Value = initialFactionId == LobbyConstants.FactionChaser ? LobbyConstants.MaskIndexChaser : 0;
        }
    }

    /// <summary>服务器设置该玩家的阵营。</summary>
    public void SetFactionServer(int id)
    {
        if (IsServer)
            factionId.Value = id;
    }

    /// <summary>服务器设置阵营与面具（大厅拾取面具时调用）。</summary>
    public void SetMaskServer(int faction, int maskIndex)
    {
        if (!IsServer) return;
        factionId.Value = faction;
        maskId.Value = maskIndex;
    }

    /// <summary>服务器设置追逐者冻结结束时间（ServerTime）。</summary>
    public void SetChaserFreezeUntilServer(double serverTimeEnd)
    {
        if (!IsServer) return;
        chaserFreezeUntil.Value = serverTimeEnd;
    }
}
