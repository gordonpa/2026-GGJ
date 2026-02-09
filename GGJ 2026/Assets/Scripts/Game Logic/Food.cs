using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 食物脚本 - 玩家可以食用的食物
/// </summary>
public class Food : NetworkBehaviour
{
    [Header("食物设置")]
    [SerializeField] private int scoreValue = 1;
    [SerializeField] private float respawnTime = 5f;
    
    private bool isConsumed = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D foodCollider;
    
    public int ScoreValue => scoreValue;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        foodCollider = GetComponent<Collider2D>();
        
        // 确保有Collider2D组件
        if (foodCollider == null)
        {
            foodCollider = gameObject.AddComponent<CircleCollider2D>();
            foodCollider.isTrigger = true;
        }
        
        // 确保有Rigidbody2D组件（Unity 2D物理系统要求至少一个对象有Rigidbody2D才能触发OnTriggerEnter2D）
        var rigidbody2D = GetComponent<Rigidbody2D>();
        if (rigidbody2D == null)
        {
            rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
            rigidbody2D.isKinematic = true; // 设置为Kinematic，不受物理影响
            rigidbody2D.gravityScale = 0; // 不受重力影响
            Debug.Log($"[Food] Added Rigidbody2D to {gameObject.name} (required for trigger detection)");
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isConsumed = false;
        SetVisible(true);
        
        // 调试信息：检查物理组件
        var rigidbody2D = GetComponent<Rigidbody2D>();
        /*Debug.Log($"[Food] OnNetworkSpawn - {gameObject.name}: " +
                  $"Has Rigidbody2D: {rigidbody2D != null}, " +
                  $"Has Collider2D: {foodCollider != null}, " +
                  $"Collider IsTrigger: {foodCollider != null && foodCollider.isTrigger}, " +
                  $"IsServer: {IsServer}");*/
    }
    
    /// <summary>
    /// 被玩家食用
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ConsumeServerRpc(ulong playerId)
    {
        Debug.Log($"[Food] ConsumeServerRpc called! PlayerId: {playerId}, IsServer: {IsServer}, IsConsumed: {isConsumed}");
        
        if (!IsServer)
        {
            Debug.LogWarning($"[Food] ConsumeServerRpc called but not on server!");
            return;
        }
        
        if (isConsumed)
        {
            Debug.Log($"[Food] Already consumed, ignoring.");
            return;
        }
        
        isConsumed = true;
        SetVisibleClientRpc(false);
        
        // 通知玩家获得积分
        var networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.SpawnManager != null)
        {
            // 通过PlayerObject查找玩家
            if (networkManager.ConnectedClients.TryGetValue(playerId, out var client))
            {
                var playerObject = client.PlayerObject;
                if (playerObject != null)
                {
                    var playerScore = playerObject.GetComponent<PlayerScore>();
                    if (playerScore != null)
                    {
                        Debug.Log($"[Food] Adding score {scoreValue} to player {playerId}");
                        playerScore.AddScoreServerRpc(scoreValue);
                    }
                    else
                    {
                        Debug.LogWarning($"[Food] Player object {playerObject.name} does not have PlayerScore component!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[Food] Player {playerId} has no PlayerObject!");
                }
            }
            else
            {
                Debug.LogWarning($"[Food] Player {playerId} not found in ConnectedClients!");
            }
        }
        
        // 延迟重新生成
        Invoke(nameof(Respawn), respawnTime);
    }
    
    /// <summary>
    /// 重新生成食物
    /// </summary>
    private void Respawn()
    {
        if (!IsServer) return;
        
        isConsumed = false;
        SetVisibleClientRpc(true);
        
        // 随机位置（在边界内）
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            Vector3 newPos = gameManager.GetRandomPositionInBounds();
            transform.position = newPos;
        }
    }
    
    /// <summary>
    /// 设置食物可见性
    /// </summary>
    [ClientRpc]
    private void SetVisibleClientRpc(bool visible)
    {
        SetVisible(visible);
    }
    
    private void SetVisible(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }
        if (foodCollider != null)
        {
            foodCollider.enabled = visible;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 调试日志
        Debug.Log($"[Food] OnTriggerEnter2D triggered! Food: {gameObject.name}, Other: {other.name}, IsServer: {IsServer}, IsConsumed: {isConsumed}");
        
        // 检查是否已被食用（客户端和服务器都要检查）
        if (isConsumed)
        {
            Debug.Log($"[Food] Already consumed, ignoring collision.");
            return;
        }
        
        // 检查是否是玩家
        var playerScore = other.GetComponent<PlayerScore>();
        if (playerScore == null)
        {
            Debug.Log($"[Food] Other object {other.name} does not have PlayerScore component.");
            return;
        }
        
        var networkObject = other.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.Log($"[Food] Other object {other.name} does not have NetworkObject component.");
            return;
        }
        
        // 检查是否是本地玩家（只有本地玩家才能触发ServerRpc）
        if (!networkObject.IsOwner)
        {
            Debug.Log($"[Food] Not owner of player object, ignoring collision.");
            return;
        }
        
        Debug.Log($"[Food] Consuming food! PlayerId: {networkObject.OwnerClientId}, IsServer: {IsServer}");
        
        // 客户端检测到碰撞后，通过ServerRpc通知服务器
        ConsumeServerRpc(networkObject.OwnerClientId);
    }
    
    /// <summary>
    /// 调试用：检查碰撞器设置
    /// </summary>
    private void OnValidate()
    {
        if (foodCollider == null)
        {
            foodCollider = GetComponent<Collider2D>();
        }
        
        if (foodCollider != null && !foodCollider.isTrigger)
        {
            Debug.LogWarning($"[Food] Collider on {gameObject.name} is not set as trigger! Setting it now.");
            foodCollider.isTrigger = true;
        }
    }
}

