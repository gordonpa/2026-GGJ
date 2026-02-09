using UnityEngine;
using Unity.Netcode;
using LayerMap;

/// <summary>
/// 客户端有玩家对象后，自动向服务器请求生成本地的 LayerMapClient（否则 Client 一直为 N，图层名不显示）。
/// 挂到场景中任意物体即可（与 LayerMapManager 同场景）。必须启用且所在物体为 Active。
/// </summary>
public class LayerMapClientBootstrap : MonoBehaviour
{
    private bool _hasRequested;

    /// <summary>GUI 调试用：是否已调用过 ReqGenClient。</summary>
    public static bool HasRequested { get; private set; }

    /// <summary>GUI 调试用：未请求时的原因（如 PlayerObject=null）。空串表示 Update 未运行（未挂载/未启用）。</summary>
    public static string NotRequestedReason { get; private set; } = "";

    private void OnEnable()
    {
        NotRequestedReason = "等待检测…";
    }

    private void Update()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsClient)
        {
            NotRequestedReason = nm == null ? "NetworkManager=null" : "非Client";
            return;
        }
        if (_hasRequested)
        {
            NotRequestedReason = "已请求过";
            return;
        }
        if (nm.LocalClient?.PlayerObject == null)
        {
            NotRequestedReason = "PlayerObject=null";
            return;
        }
        if (LayerMapManager.Instance == null)
        {
            NotRequestedReason = "LayerMapManager.Instance=null";
            return;
        }
        if (LayerMapManager.Instance.Client != null)
        {
            NotRequestedReason = "Client已存在";
            return;
        }

        NotRequestedReason = "";
        LayerMapManager.Instance.ReqGenClient();
        _hasRequested = true;
        HasRequested = true;
    }
}
