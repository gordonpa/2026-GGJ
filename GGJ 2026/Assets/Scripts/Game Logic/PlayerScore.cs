using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 玩家积分系统 - 管理玩家的积分和名称
/// </summary>
public class PlayerScore : NetworkBehaviour
{
    private NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public int Score => score.Value;
    public ulong PlayerId => GetComponent<NetworkObject>().OwnerClientId;
    public string PlayerName => playerName.Value.ToString();
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 监听积分变化
        score.OnValueChanged += OnScoreChanged;
        
        // 如果还没有设置名称，使用默认名称
        if (IsOwner && string.IsNullOrEmpty(playerName.Value.ToString()))
        {
            SetPlayerNameServerRpc($"玩家 {PlayerId}");
        }
        
        // 监听名称变化
        playerName.OnValueChanged += OnNameChanged;
    }
    
    public override void OnNetworkDespawn()
    {
        score.OnValueChanged -= OnScoreChanged;
        playerName.OnValueChanged -= OnNameChanged;
        base.OnNetworkDespawn();
    }
    
    /// <summary>
    /// 添加积分（服务器端）
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRpc(int points)
    {
        score.Value += points;
    }
    
    /// <summary>
    /// 设置积分（服务器端）
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetScoreServerRpc(int newScore)
    {
        score.Value = newScore;
    }
    
    /// <summary>
    /// 设置玩家名称（只有Owner可以设置）
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void SetPlayerNameServerRpc(FixedString64Bytes newName)
    {
        playerName.Value = newName;
    }
    
    /// <summary>
    /// 设置玩家名称（公共方法，供外部调用）
    /// </summary>
    public void SetPlayerName(string name)
    {
        if (IsOwner && !string.IsNullOrEmpty(name))
        {
            // 限制名称长度
            if (name.Length > 64)
            {
                name = name.Substring(0, 64);
            }
            SetPlayerNameServerRpc(new FixedString64Bytes(name));
        }
    }
    
    /// <summary>
    /// 积分变化回调
    /// </summary>
    private void OnScoreChanged(int oldValue, int newValue)
    {
        // 通知积分管理器更新UI
        var scoreManager = FindObjectOfType<ScoreboardUI>();
        if (scoreManager != null)
        {
            scoreManager.UpdateScoreboard();
        }
    }
    
    /// <summary>
    /// 名称变化回调
    /// </summary>
    private void OnNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        // 更新GameObject名称
        if (!string.IsNullOrEmpty(newValue.ToString()))
        {
            gameObject.name = $"{newValue} (Player {PlayerId})";
        }
        
        // 通知积分管理器更新UI
        var scoreManager = FindObjectOfType<ScoreboardUI>();
        if (scoreManager != null)
        {
            scoreManager.UpdateScoreboard();
        }
    }
}

