using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 食物生成器 - 在有边界的场地中生成食物
/// </summary>
public class FoodSpawner : NetworkBehaviour
{
    [Header("生成设置")]
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private int initialFoodCount = 20;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxFoodCount = 50;
    
    [Header("边界设置")]
    [SerializeField] private Vector2 boundsMin = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 boundsMax = new Vector2(10f, 10f);
    
    private int currentFoodCount = 0;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsServer) return;
        
        // 初始生成食物
        for (int i = 0; i < initialFoodCount; i++)
        {
            SpawnFood();
        }
        
        // 定期生成食物
        InvokeRepeating(nameof(CheckAndSpawnFood), spawnInterval, spawnInterval);
    }
    
    public override void OnNetworkDespawn()
    {
        CancelInvoke();
        base.OnNetworkDespawn();
    }
    
    /// <summary>
    /// 检查并生成食物
    /// </summary>
    private void CheckAndSpawnFood()
    {
        if (!IsServer) return;
        
        // 统计当前食物数量
        currentFoodCount = FindObjectsOfType<Food>().Length;
        
        // 如果食物数量不足，生成新食物
        if (currentFoodCount < maxFoodCount)
        {
            SpawnFood();
        }
    }
    
    /// <summary>
    /// 生成一个食物
    /// </summary>
    private void SpawnFood()
    {
        if (foodPrefab == null)
        {
            Debug.LogError("FoodSpawner: 未设置食物预制体！");
            return;
        }
        
        Vector3 spawnPosition = GetRandomPositionInBounds();
        GameObject food = Instantiate(foodPrefab, spawnPosition, Quaternion.identity);
        
        // 生成网络对象
        NetworkObject foodNetworkObject = food.GetComponent<NetworkObject>();
        if (foodNetworkObject != null)
        {
            foodNetworkObject.Spawn();
        }
    }
    
    /// <summary>
    /// 获取边界内的随机位置
    /// </summary>
    public Vector3 GetRandomPositionInBounds()
    {
        float x = Random.Range(boundsMin.x, boundsMax.x);
        float y = Random.Range(boundsMin.y, boundsMax.y);
        return new Vector3(x, y, 0f);
    }
    
    /// <summary>
    /// 设置边界
    /// </summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        boundsMin = min;
        boundsMax = max;
    }
    
    /// <summary>
    /// 获取边界
    /// </summary>
    public void GetBounds(out Vector2 min, out Vector2 max)
    {
        min = boundsMin;
        max = boundsMax;
    }
    
    private void OnDrawGizmos()
    {
        // 在Scene视图中绘制边界
        Gizmos.color = Color.green;
        Vector3 center = new Vector3((boundsMin.x + boundsMax.x) / 2f, (boundsMin.y + boundsMax.y) / 2f, 0f);
        Vector3 size = new Vector3(boundsMax.x - boundsMin.x, boundsMax.y - boundsMin.y, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}

