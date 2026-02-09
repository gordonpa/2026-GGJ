using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 挂在玩家预制体上，表示当前携带的任务物品。-1 表示未携带，0/1/2... 表示物品 ID。
/// </summary>
public class CarriedItemHolder : NetworkBehaviour
{
    public const int NoItemId = -1;

    private NetworkVariable<int> carriedItemId = new NetworkVariable<int>(NoItemId,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int CarriedItemId => carriedItemId.Value;
    /// <summary>是否携带任意任务物品（含 itemId=0 的第一个物品）。</summary>
    public bool HasItem => carriedItemId.Value >= 0;
    /// <summary>供 CarriedItemVisual 等订阅携带变化。</summary>
    public NetworkVariable<int> CarriedItemIdVariable => carriedItemId;

    /// <summary>服务器：设置携带的物品 ID（拾取时调用）。</summary>
    public void SetCarriedItemServer(int itemId)
    {
        if (IsServer)
            carriedItemId.Value = itemId;
    }

    /// <summary>服务器：清空携带（提交时调用）。</summary>
    public void ClearCarriedItemServer()
    {
        if (IsServer)
            carriedItemId.Value = NoItemId;
    }
}
