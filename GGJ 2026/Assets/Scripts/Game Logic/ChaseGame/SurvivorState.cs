using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 求生者状态：是否死亡、携带的死亡面具（可多个）、普通技能被禁用截止时间。挂玩家预制体。
/// 服务器用位掩码存携带面具；每次变更后通过 ClientRpc 推给该客户端，客户端用本地缓存驱动 J 选项（不依赖 NetworkVariable 同步）。
/// </summary>
[RequireComponent(typeof(FactionMember))]
public class SurvivorState : NetworkBehaviour
{
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    /// <summary>服务器：携带的死亡面具位掩码 bit0/1/2。</summary>
    private NetworkVariable<int> carriedDeadMaskBits = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private NetworkVariable<double> skillDisabledUntil = new NetworkVariable<double>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    /// <summary>客户端本地缓存：由 SyncCarriedMaskBitsClientRpc 更新，UI 只读此值，避免 NetworkVariable 不同步。</summary>
    private int _clientCarriedMaskBits;

    public bool IsDead => isDead.Value;
    /// <summary>服务器读：当前携带的死亡面具位掩码。击杀时掉落物用此值做继承。</summary>
    public int CarriedDeadMaskBitsValue => carriedDeadMaskBits.Value;
    public bool HasCarriedDeadMask => GetCarriedBits() != 0;
    public int CarriedDeadMaskIndex => FirstCarriedDeadMaskIndex(GetCarriedBits());
    public double SkillDisabledUntil => skillDisabledUntil.Value;

    /// <summary>客户端收到新携带状态时触发。</summary>
    public event System.Action CarriedDeadMaskBitsChanged;

    private int GetCarriedBits()
    {
        if (IsServer) return carriedDeadMaskBits.Value;
        return _clientCarriedMaskBits;
    }

    /// <summary>已携带的死亡面具索引列表（0/1/2），用于 J 技能选项。</summary>
    public List<int> GetCarriedDeadMaskIndices()
    {
        var list = new List<int>();
        int bits = GetCarriedBits();
        for (int i = 0; i <= 2; i++)
            if ((bits & (1 << i)) != 0) list.Add(i);
        return list;
    }

    private static int FirstCarriedDeadMaskIndex(int bits)
    {
        for (int i = 0; i <= 2; i++)
            if ((bits & (1 << i)) != 0) return i;
        return -1;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            _clientCarriedMaskBits = carriedDeadMaskBits.Value;
            SyncCarriedMaskBitsClientRpc(carriedDeadMaskBits.Value, ClientRpcParamsForOwner());
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    public bool IsSkillDisabled()
    {
        if (skillDisabledUntil.Value <= 0) return false;
        var nm = NetworkManager.Singleton;
        return nm != null && nm.ServerTime.Time < skillDisabledUntil.Value;
    }

    public void SetDeadServer(bool dead)
    {
        if (IsServer)
            isDead.Value = dead;
    }

    /// <summary>服务器：拾取一个死亡面具时追加，并立即推给该客户端。</summary>
    public void AddCarriedDeadMaskServer(int maskIndex)
    {
        if (!IsServer) return;
        if (maskIndex < 0 || maskIndex > 2) return;
        carriedDeadMaskBits.Value |= (1 << maskIndex);
        SyncCarriedMaskBitsClientRpc(carriedDeadMaskBits.Value, ClientRpcParamsForOwner());
        GameLogGUI.AddLine($"[面具] AddCarriedDeadMask maskIndex={maskIndex} bits={carriedDeadMaskBits.Value}");
    }

    /// <summary>服务器：拾取掉落物时追加一整段位掩码（掉落物可能继承多人面具），并立即推给该客户端。</summary>
    public void AddCarriedDeadMaskBitsServer(int bits)
    {
        if (!IsServer) return;
        int add = bits & 7; // 只取 bit0/1/2
        if (add == 0) return;
        carriedDeadMaskBits.Value |= add;
        SyncCarriedMaskBitsClientRpc(carriedDeadMaskBits.Value, ClientRpcParamsForOwner());
        GameLogGUI.AddLine($"[面具] AddCarriedDeadMaskBits bits={add} 当前={carriedDeadMaskBits.Value}");
    }

    private ClientRpcParams ClientRpcParamsForOwner()
    {
        return new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } } };
    }

    [ClientRpc]
    private void SyncCarriedMaskBitsClientRpc(int bits, ClientRpcParams rpcParams = default)
    {
        _clientCarriedMaskBits = bits;
        CarriedDeadMaskBitsChanged?.Invoke();
    }

    public void SetSkillDisabledUntilServer(double serverTimeEnd)
    {
        if (IsServer)
            skillDisabledUntil.Value = serverTimeEnd;
    }
}
