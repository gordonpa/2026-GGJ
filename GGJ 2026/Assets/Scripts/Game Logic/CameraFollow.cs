using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 相机跟随脚本 - 每个玩家有自己的相机，跟随自己移动
/// </summary>
public class CameraFollow : NetworkBehaviour
{
    [Header("跟随设置")]
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 5f;
    
    [Header("相机缩放设置")]
    [SerializeField] private float cameraSize = 5f;
    [SerializeField] private float minSize = 3f;
    [SerializeField] private float maxSize = 10f;
    
    private Camera cam;
    
    public override void OnNetworkSpawn()
    {
        // 只为本地的玩家对象设置相机
        if (!IsOwner) return;
        
        cam = Camera.main;
        if (cam == null)
        {
            // 如果没有主相机，创建一个
            GameObject cameraObj = new GameObject("PlayerCamera");
            cam = cameraObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        
        // 设置目标为自己
        if (target == null)
        {
            target = transform;
        }
        
        // 设置初始相机大小
        if (cam.orthographic)
        {
            cam.orthographicSize = cameraSize;
        }
        
        base.OnNetworkSpawn();
    }
    
    private void LateUpdate()
    {
        // 只为本地的玩家对象更新相机
        if (!IsOwner || cam == null || target == null) return;
        
        // 平滑跟随目标
        Vector3 targetPos = new Vector3(target.position.x, target.position.y, cam.transform.position.z);
        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, followSpeed * Time.deltaTime);
        
        // 更新相机大小
        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Clamp(cameraSize, minSize, maxSize);
        }
    }
    
    /// <summary>
    /// 设置相机缩放大小
    /// </summary>
    public void SetCameraSize(float size)
    {
        cameraSize = Mathf.Clamp(size, minSize, maxSize);
    }
    
    /// <summary>
    /// 获取当前相机大小
    /// </summary>
    public float GetCameraSize()
    {
        return cameraSize;
    }
}

