using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(FactionMember))] // 强制依赖阵营组件
public class Shadow_Hide : NetworkBehaviour
{
    [Header("阴影检测")]
    public string shadowTag = "Shadow";

    [Header("透明度设置")]
    [Tooltip("逃生者之间的透明度（看自己和看队友都是此值，默认0.5）")]
    [Range(0f, 1f)] public float survivorToSurvivorAlpha = 0.5f;

    [Tooltip("抓捕者看逃生者的透明度（默认0，即完全隐身）")]
    [Range(0f, 1f)] public float chaserToSurvivorAlpha = 0f;

    [Header("过渡速度")]
    public float fadeSpeed = 10f;

    // 只同步“是否在阴影中”，身份信息直接由 FactionMember 同步
    private NetworkVariable<bool> inShadow = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
    private List<float> originalAlphas = new List<float>();
    private float currentAlpha = 1f;
    private float targetAlpha = 1f;

    // 缓存组件
    private FactionMember myFaction;

    void Awake()
    {
        myFaction = GetComponent<FactionMember>();

        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            spriteRenderers.Add(sr);
            originalAlphas.Add(sr.color.a);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        inShadow.OnValueChanged += (_, _) => UpdateTargetAlpha();

        // 初始检查一次
        UpdateTargetAlpha();
    }

    void Update()
    {
        // 持续平滑过渡
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
        ApplyAlpha(currentAlpha);

        // 可选：如果阵营可能会在游戏中途切换，建议在这里偶尔检查一下 TargetAlpha
        // 或者在 FactionMember 里有事件的话订阅事件更好。
        // 为了保险起见，每帧检查一次逻辑状态（开销极小）
        if (inShadow.Value) UpdateTargetAlpha();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 只有 Owner 负责检测触发器，并通知服务器
        if (!IsOwner || !other.CompareTag(shadowTag)) return;
        SetShadowStateServerRpc(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsOwner || !other.CompareTag(shadowTag)) return;
        SetShadowStateServerRpc(false);
    }

    [ServerRpc(RequireOwnership = false)]
    void SetShadowStateServerRpc(bool state)
    {
        inShadow.Value = state;
    }

    void UpdateTargetAlpha()
    {
        // 1. 如果不在阴影里，永远完全显示
        if (!inShadow.Value)
        {
            targetAlpha = 1f;
            return;
        }

        // 2. 获取被观察者（我）的身份
        bool amIChaser = false;
        if (myFaction != null) amIChaser = myFaction.IsChaser;

        // 规则：抓捕者进草丛永远不隐身（对任何人都可见）
        if (amIChaser)
        {
            targetAlpha = 1f;
            return;
        }

        // --- 以下逻辑针对：我是逃生者，且我在草丛里 ---

        // 3. 获取观察者（本地客户端玩家）的身份
        var localClient = NetworkManager.Singleton?.LocalClient;
        var localPlayerObj = localClient?.PlayerObject;

        // 如果没有本地玩家（比如纯Server模式），或者本地玩家就是我自己 -> 半透明
        if (localPlayerObj == null || localPlayerObj == this.gameObject)
        {
            targetAlpha = survivorToSurvivorAlpha;
            return;
        }

        // 获取观察者的阵营
        var observerFaction = localPlayerObj.GetComponent<FactionMember>();
        bool observerIsChaser = false;
        if (observerFaction != null) observerIsChaser = observerFaction.IsChaser;

        // 4. 根据观察者身份决定透明度
        if (observerIsChaser)
        {
            // 抓捕者看逃生者 -> 完全隐形
            targetAlpha = chaserToSurvivorAlpha;
        }
        else
        {
            // 队友看逃生者 -> 半透明
            targetAlpha = survivorToSurvivorAlpha;
        }
    }

    void ApplyAlpha(float alpha)
    {
        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            if (spriteRenderers[i] == null) continue;
            var col = spriteRenderers[i].color;
            // 基于原始透明度进行乘法，防止原本就是半透明的物体变得不透明
            col.a = originalAlphas[i] * alpha;
            spriteRenderers[i].color = col;
        }
    }
}