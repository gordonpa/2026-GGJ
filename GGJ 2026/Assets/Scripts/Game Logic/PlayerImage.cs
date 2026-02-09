using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家视觉管理：根据MaskId切换Sprite和Animator，实现单预制体多角色
/// </summary>
public class PlayerImage : NetworkBehaviour
{
    [Header("外观配置（按MaskId索引 0-3）")]
    [Tooltip("MaskId 0-2: 求生者A/B/C, MaskId 3: 抓捕者")]
    [SerializeField] private PlayerVisualData[] visualData = new PlayerVisualData[4];

    [Header("默认大厅外观")]
    [SerializeField] private Sprite defaultLobbySprite;
    [SerializeField] private RuntimeAnimatorController defaultLobbyAnimator; // 大厅待机动画

    [Header("组件引用")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    // 网络同步当前外观ID（-1表示未初始化，0-3对应不同角色）
    private NetworkVariable<int> currentVisualId = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private FactionMember factionMember;
    private int lastMaskId = -1;

    [System.Serializable]
    public struct PlayerVisualData
    {
        public string roleName;                    // 角色名（调试用）
        public Sprite idleSprite;                  // 待机图（可选，如果第一帧就是动画可不填）
        public RuntimeAnimatorController controller; // 专属动画控制器（必须）
        public float moveSpeed;                    // 该角色的移动速度（可选）
        public Color roleColor;                    // 角色标识色
    }

    void Awake()
    {
        // 自动获取组件（确保和PlayerMovement用的是同一个）
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        factionMember = GetComponent<FactionMember>();

        // 监听面具变化
        if (factionMember != null)
        {
            factionMember.MaskIdVariable.OnValueChanged += OnMaskChanged;
            // 初始刷新
            RefreshVisual(factionMember.MaskId);
        }

        // 监听视觉ID变化（用于网络同步）
        currentVisualId.OnValueChanged += OnVisualIdChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (factionMember != null)
            factionMember.MaskIdVariable.OnValueChanged -= OnMaskChanged;
        currentVisualId.OnValueChanged -= OnVisualIdChanged;
        base.OnNetworkDespawn();
    }

    private void OnMaskChanged(int oldMask, int newMask)
    {
        // 只在Server端更新权威数据，自动同步到所有客户端
        if (IsServer)
        {
            currentVisualId.Value = newMask;
        }
    }

    private void OnVisualIdChanged(int oldId, int newId)
    {
        if (newId >= 0 && newId < visualData.Length)
        {
            ApplyVisualData(newId);
        }
    }

    /// <summary>
    /// 强制刷新当前视觉（Server调用或本地初始化）
    /// </summary>
    public void RefreshVisual(int maskId)
    {
        if (IsServer)
        {
            currentVisualId.Value = maskId;
        }
        ApplyVisualData(maskId);
    }

    private void ApplyVisualData(int index)
    {
        if (index < 0 || index >= visualData.Length) return;

        var data = visualData[index];

        // 1. 切换Sprite（第一帧静态图，可选）
        if (data.idleSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = data.idleSprite;
        }

        // 2. 切换Animator
        if (animator != null && data.controller != null)
        {
            animator.runtimeAnimatorController = data.controller;
            // 重置动画状态，避免上一个角色的动画残留
            animator.Rebind();
            animator.Update(0f);
        }

        // 3. 可选：通知其他脚本视觉已变更
        OnVisualChanged?.Invoke(index, data);

        Debug.Log($"[PlayerImage] 玩家 {OwnerClientId} 切换为 {data.roleName} (MaskId: {index})");
    }

    // 事件：当视觉切换完成时触发（供其他脚本订阅）
    public event Action<int, PlayerVisualData> OnVisualChanged;

    // ==================== 便捷方法 ====================

    /// <summary>
    /// 获取当前动画控制器（供PlayerMovement使用）
    /// </summary>
    public Animator GetAnimator() => animator;

    /// <summary>
    /// 获取当前配置数据
    /// </summary>
    public PlayerVisualData GetCurrentData()
    {
        if (currentVisualId.Value < 0 || currentVisualId.Value >= visualData.Length)
            return default;
        return visualData[currentVisualId.Value];
    }

    /// <summary>
    /// 是否已应用面具（非默认状态）
    /// </summary>
    public bool HasVisualApplied => currentVisualId.Value >= 0;
}