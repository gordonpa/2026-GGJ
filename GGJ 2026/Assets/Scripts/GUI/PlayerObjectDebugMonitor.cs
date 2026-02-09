using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 调试用：每 3 秒检查本地 PlayerObject 是否存在、是否 active、LayerInteractInput 是否在运行。
/// 挂到场景中任意物体（与 GameLogGUI 同场景）即可。
/// </summary>
public class PlayerObjectDebugMonitor : MonoBehaviour
{
    private float lastCheckTime = -999f;
    private int updateCallCount = 0;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        updateCallCount++;
        if (updateCallCount > 10 && (updateCallCount - 10) % 90 != 0) return; // 只在第 10 帧后每 90 帧（约 3 秒）打一次

        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsClient) return;
        if (nm.LocalClient == null) return;

        var playerObj = nm.LocalClient.PlayerObject;
        if (playerObj == null) return;

        var layerInteract = playerObj.GetComponent<LayerInteractInput>();
        if (layerInteract == null)
        {
            GameLogGUI.AddLine($"[PlayerObjectDebug] WARNING: PlayerObject 没有 LayerInteractInput!");
        }
    }
}
