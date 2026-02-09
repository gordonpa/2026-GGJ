using Unity.Netcode;
using UnityEngine;
using GameJam.LayerTask; // 引用阵营相关逻辑
using LayerMap;          // 引用 LayerTaskManager

/// <summary>
/// 挂在"任务物品"预制体上。
/// 负责同步物品的图层ID、物品ID，并处理拾取逻辑。
/// 集成了强制同步修复，防止客户端 LayerID 为 0 的问题。
/// </summary>
public class LayerCollectible : NetworkBehaviour
{
    [Header("调试与配置")]
    // 🔧 1. 暂存变量：用于在 Spawn 前接收 ItemSpawner 的赋值
    public int initialLayerId = 0;
    public int initialItemId = 0;

    // 🔧 2. 网络变量：确保数据从服务器同步到客户端
    // 默认值设为 0，权限为 Server
    private NetworkVariable<int> layerIdNet = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> itemIdNet = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("属性设置")]
    [Tooltip("可拾取该物品的阵营 ID，0 表示任意阵营")]
    [SerializeField] private int factionIdAllowed = 0;

    [Tooltip("物品唯一ID，用于持久化（场景重载后保持被捡走的状态）")]
    [SerializeField] private string uniqueItemId;

    // 状态变量：是否已被捡走 (true = 隐藏/不可交互)
    private NetworkVariable<bool> isPickedUp = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // 对外公开属性 (读取网络变量)
    public int LayerId => layerIdNet.Value;
    public int ItemId => itemIdNet.Value;
    public bool IsPickedUp => isPickedUp.Value;
    public string UniqueItemId => uniqueItemId;

    private void Awake()
    {
        // 自动生成唯一ID（如果未配置），用于跨场景/重连识别
        if (string.IsNullOrEmpty(uniqueItemId))
        {
            string posX = transform.position.x.ToString("F2");
            string posY = transform.position.y.ToString("F2");
            uniqueItemId = $"{gameObject.scene.name}_{gameObject.name}_{posX}_{posY}"
                           .Replace("(", "").Replace(")", "").Replace(" ", "_").Replace(".", "p");
        }
    }

    /// <summary>
    /// 供 ItemSpawner 在实例化后立即调用。
    /// 只修改普通变量，不涉及网络，确保绝对成功。
    /// </summary>
    public void Setup(int item, int layer, int faction)
    {
        initialItemId = item;
        initialLayerId = layer;
        factionIdAllowed = faction;
        gameObject.name = $"Item{item}_Layer{layer}";
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 🔧 3. 服务器负责初始化网络变量
        if (IsServer)
        {
            // 将"保险箱"里的值写入网络变量，NGO 会自动同步给所有人
            if (initialLayerId != 0) layerIdNet.Value = initialLayerId;
            if (initialItemId != 0) itemIdNet.Value = initialItemId;

            // 检查持久化数据（如果物品之前被捡过，现在要保持隐藏）
            if (!string.IsNullOrEmpty(uniqueItemId) && LayerTaskManager.IsItemPickedUp(uniqueItemId))
            {
                isPickedUp.Value = true;
            }
        }

        // 客户端和服务器都执行：根据当前状态更新显示
        ApplyPickedUpVisuals(isPickedUp.Value);

        // 注册回调：状态变化时自动更新显示
        isPickedUp.OnValueChanged += OnPickedUpStateChanged;
    }

    // 🔥🔥🔥 关键修复：强制同步补救 🔥🔥🔥
    // 如果 OnNetworkSpawn 时同步失败，Update 会在每一帧检查并修正
    private void Update()
    {
        if (IsServer)
        {
            // 如果 NetworkVariable 还是 0，但我们明确知道初始值不是 0
            // 说明之前的赋值可能没生效（比如 Spawn 时序问题），这里强制再赋一次
            if (layerIdNet.Value == 0 && initialLayerId != 0)
            {
                // Debug.LogWarning($"[Server] 补救同步 LayerId: 0 -> {initialLayerId}");
                layerIdNet.Value = initialLayerId;
            }

            if (itemIdNet.Value == 0 && initialItemId != 0)
            {
                itemIdNet.Value = initialItemId;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isPickedUp.OnValueChanged -= OnPickedUpStateChanged;
    }

    /// <summary>
    /// 客户端调用：核心判定逻辑，检查是否可以被当前玩家捡起
    /// </summary>
    public bool CanBePickedBy(CarriedItemHolder holder, FactionMember faction, int playerLayerId)
    {
        // 1. 基础检查：是否被捡走、玩家手是否满了
        if (isPickedUp.Value) return false;
        if (holder == null || holder.HasItem) return false;

        // 2. 阵营检查
        if (faction != null && faction.IsChaser) return false; // 追捕者不能捡
        if (factionIdAllowed != 0 && (faction == null || faction.FactionId != factionIdAllowed)) return false;

        // 3. 🔥 图层检查
        // 现在有了 Update 里的强制修复，layerIdNet.Value 应该是正确的了
        if (playerLayerId != LayerId)
        {
            // 如果还进到这里，说明真的是图层不对，或者同步彻底断了
            Debug.LogError($"[拾取失败] 图层不匹配! 玩家层级:{playerLayerId} vs 物品层级:{LayerId} (ItemNet:{itemIdNet.Value})");
            return false;
        }

        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void PickUpServerRpc(ulong playerClientId)
    {
        if (!IsServer || isPickedUp.Value) return;

        // 获取请求玩家的对象
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(playerClientId, out var client)) return;
        var playerObject = client.PlayerObject;
        if (playerObject == null) return;

        // 获取组件
        var holder = playerObject.GetComponent<CarriedItemHolder>();
        var faction = playerObject.GetComponent<FactionMember>();

        // 服务器端再次验证逻辑
        if (holder == null || holder.HasItem) return;
        if (faction != null && faction.FactionId == LobbyConstants.FactionChaser) return;

        // ✅ 拾取成功逻辑
        isPickedUp.Value = true; // 标记为已捡走，触发 OnValueChanged 隐藏物体

        // 持久化记录
        if (!string.IsNullOrEmpty(uniqueItemId))
        {
            LayerTaskManager.MarkItemPickedUp(uniqueItemId);
        }

        // 将物品 ID 给玩家 (Holder)
        holder.SetCarriedItemServer(ItemId);

        // 更新玩家头顶的显示 (Visual)
        var carriedVisual = playerObject.GetComponentInChildren<CarriedItemVisual>(true);
        if (carriedVisual != null)
        {
            carriedVisual.RefreshCarriedVisualClientRpc(ItemId);
            carriedVisual.ApplyCarriedVisual(ItemId);
        }
    }

    private void OnPickedUpStateChanged(bool oldVal, bool newVal)
    {
        ApplyPickedUpVisuals(newVal);
    }

    private void ApplyPickedUpVisuals(bool pickedUp)
    {
        // 被捡走了就隐藏 (SetActive false)，没捡走就显示
        gameObject.SetActive(!pickedUp);
    }

    private void OnValidate()
    {
        // 确保 Collider 是 Trigger，否则玩家会撞到物品而不是穿过去
        var c = GetComponent<Collider2D>();
        if (c != null && !c.isTrigger) c.isTrigger = true;
    }
}