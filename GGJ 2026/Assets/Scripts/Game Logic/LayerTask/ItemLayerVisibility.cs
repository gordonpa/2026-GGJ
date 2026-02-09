using LayerMap;
using Unity.Netcode;
using UnityEngine;

public class ItemLayerVisibility : NetworkBehaviour
{
    // 1. 暂存变量 (保险箱)
    public MapLayer initialLayer = MapLayer.Main;

    // 2. 网络变量
    private NetworkVariable<MapLayer> belongLayer = new NetworkVariable<MapLayer>(
        MapLayer.Main,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private SpriteRenderer sr;
    private Collider2D col;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            belongLayer.Value = initialLayer;
        }
        UpdateVisibility();
    }

    void Update()
    {
        // 🔥 1. 服务器强制纠错 (核弹修复)
        // 如果 NetworkVariable 还是 Main (默认值)，但初始值不是 Main，强制覆盖！
        if (IsServer)
        {
            if (belongLayer.Value == MapLayer.Main && initialLayer != MapLayer.Main)
            {
                belongLayer.Value = initialLayer;
            }
        }

        // 2. 持续刷新显示
        // 防止网络延迟导致 OnValueChanged 没触发，或者玩家切换图层后显示没更新
        UpdateVisibility();
    }

    void UpdateVisibility()
    {
        if (!IsSpawned) return;

        // 如果被捡走了，由 LayerCollectible 控制隐藏
        var collectible = GetComponent<LayerCollectible>();
        if (collectible != null && collectible.IsPickedUp)
        {
            SetVisible(false);
            return;
        }

        if (LayerMapManager.Instance == null || LayerMapManager.Instance.Client == null)
            return;

        // 获取当前图层
        MapLayer myLayer = LayerMapManager.Instance.Client.Layer.Value;
        MapLayer itemLayer = belongLayer.Value;

        // 🔥 核心判定：图层一致才显示 + 开碰撞
        // 如果这里 itemLayer 是 Main(0) 而你是 Layer2，shouldShow 就是 false
        // 你的碰撞体就会被关掉，导致捡不起来！
        bool shouldShow = (itemLayer == myLayer);

        SetVisible(shouldShow);
    }

    private void SetVisible(bool show)
    {
        if (sr != null && sr.enabled != show) sr.enabled = show;
        if (col != null && col.enabled != show) col.enabled = show;
    }
}