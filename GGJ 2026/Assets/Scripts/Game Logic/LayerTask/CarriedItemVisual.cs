using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 携带视觉：只负责显示/隐藏“携带中的物体”。由 LayerCollectible / LayerSubmitZone 在拾取/提交时调用 ApplyCarriedVisual；
/// 同时根据 CarriedItemHolder.HasItem 在每帧强制隐藏未携带时的显示。
/// 挂在玩家预制体（根或子物体）上，配置 Carried Visual Root（玩家子物体或预制体）。
/// 约定：carriedItemId >= 0 表示显示对应物品（0=第一个物品），-1 表示隐藏。
/// </summary>
public class CarriedItemVisual : NetworkBehaviour
{
    /// <summary>隐藏携带视觉时传入此值。0 表示“第一个物品”，不是“无物品”。</summary>
    public const int HideItemId = -1;
    [Header("携带视觉")]
    [Tooltip("要显示的物体：拖玩家下的子物体，或 Project 里的预制体（将自动实例化并挂在本物体下）")]
    [SerializeField] private GameObject carriedVisualRoot;
    [Tooltip("按 ItemId 切换图标，索引对应 ItemId")]
    [SerializeField] private Sprite[] spritesByItemId;

    private GameObject _instance;
    private Transform _parentForInstance;
    private CarriedItemHolder _holder;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _parentForInstance = transform;
        _holder = GetComponentInParent<CarriedItemHolder>();
        EnsureVisualExistsAndHide();
    }

    private void LateUpdate()
    {
        if (_holder != null && !_holder.HasItem)
            ApplyCarriedVisual(HideItemId);
    }

    public override void OnNetworkDespawn()
    {
        if (_instance != null)
        {
            Destroy(_instance);
            _instance = null;
        }
        base.OnNetworkDespawn();
    }

    /// <summary>ClientRpc：客户端刷新显示。Host 可能不执行自己的 ClientRpc，所以服务器会再调 ApplyCarriedVisual。</summary>
    [ClientRpc]
    public void RefreshCarriedVisualClientRpc(int carriedItemId)
    {
        ApplyCarriedVisual(carriedItemId);
    }

    /// <summary>直接设置携带视觉显示/隐藏。服务器在发完 ClientRpc 后调用一次，保证 Host 也能看到。carriedItemId >= 0 显示该物品（0=第一个），HideItemId(-1) 隐藏。</summary>
    public void ApplyCarriedVisual(int carriedItemId)
    {
        bool show = carriedItemId >= 0;
        GameObject root = GetOrCreateRoot();
        if (root == null)
        {
            if (show) Debug.LogWarning("[CarriedItemVisual] Carried Visual Root 未配置或实例化失败");
            return;
        }

        if (show)
        {
            root.SetActive(true);
            SetActiveRecursive(root, true);
            SpriteRenderer sr = root.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                sr.enabled = true;
                if (spritesByItemId != null && carriedItemId < spritesByItemId.Length && spritesByItemId[carriedItemId] != null)
                    sr.sprite = spritesByItemId[carriedItemId];
            }
        }
        else
        {
            root.SetActive(false);
        }
    }

    private void EnsureVisualExistsAndHide()
    {
        GameObject root = GetOrCreateRoot();
        if (root != null)
            root.SetActive(false);
    }

    private GameObject GetOrCreateRoot()
    {
        if (carriedVisualRoot == null) return null;

        if (carriedVisualRoot.scene.IsValid())
            return carriedVisualRoot;

        if (_instance == null && _parentForInstance != null)
        {
            _instance = Instantiate(carriedVisualRoot, _parentForInstance);
            _instance.name = carriedVisualRoot.name + "(Carried)";
            _instance.transform.localPosition = Vector3.zero;
            _instance.transform.localRotation = Quaternion.identity;
            _instance.transform.localScale = Vector3.one;
            _instance.SetActive(false);
        }
        return _instance;
    }

    private static void SetActiveRecursive(GameObject go, bool active)
    {
        go.SetActive(active);
        for (int i = 0; i < go.transform.childCount; i++)
            SetActiveRecursive(go.transform.GetChild(i).gameObject, active);
    }
}
