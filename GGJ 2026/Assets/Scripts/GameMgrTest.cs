using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 游戏管理器 - 管理游戏边界和基本规则
/// </summary>
public class GameMgrTest : NetworkBehaviour
{
    [Header("游戏边界设置")]
    [SerializeField] private Vector2 boundsMin = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 boundsMax = new Vector2(20f, 20f);
    
    [Header("边界可视化")]
    [SerializeField] private bool showBounds = true;
    [SerializeField] private Color boundsColor = Color.red;
    
    private static GameMgrTest instance;
    public static GameMgrTest Instance => instance;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
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
    /// 检查位置是否在边界内
    /// </summary>
    public bool IsPositionInBounds(Vector3 position)
    {
        return position.x >= boundsMin.x && position.x <= boundsMax.x &&
               position.y >= boundsMin.y && position.y <= boundsMax.y;
    }
    
    /// <summary>
    /// 将位置限制在边界内
    /// </summary>
    public Vector3 ClampPositionToBounds(Vector3 position)
    {
        float x = Mathf.Clamp(position.x, boundsMin.x, boundsMax.x);
        float y = Mathf.Clamp(position.y, boundsMin.y, boundsMax.y);
        return new Vector3(x, y, position.z);
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
        if (!showBounds) return;
        
        // 在Scene视图中绘制边界
        Gizmos.color = boundsColor;
        Vector3 center = new Vector3((boundsMin.x + boundsMax.x) / 2f, (boundsMin.y + boundsMax.y) / 2f, 0f);
        Vector3 size = new Vector3(boundsMax.x - boundsMin.x, boundsMax.y - boundsMin.y, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}

