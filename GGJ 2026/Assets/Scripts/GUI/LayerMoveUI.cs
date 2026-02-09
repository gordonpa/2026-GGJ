using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;
using LayerMap;
using System.Collections.Generic;

/// <summary>
/// 图层移动选择 UI：J 键打开，显示可选的图层按钮（追逐者 4 个，求生者 2～3 个），点击后请求切换并进入 CD。
/// 需绑定 LayerMoveAbility 与若干按钮（或由代码生成）。
/// </summary>
public class LayerMoveUI : MonoBehaviour
{
    [Header("绑定")]
    [Tooltip("图层移动能力（玩家预制体上）")]
    [SerializeField] private LayerMoveAbility layerMoveAbility;
    [Tooltip("打开图层选择的按键")]
    [SerializeField] private KeyCode openKey = KeyCode.J;
    [Tooltip("面板根（显示/隐藏）。四个图层按钮应作为其子物体；Start 时会 SetActive(false)，进游戏时默认不显示")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("按钮容器：按顺序 Main, Layer1, Layer2, Layer3")]
    [SerializeField] private List<Button> layerButtons = new List<Button>();
    [Tooltip("按钮文案：主图层、图层1、图层2、图层3")]
    [SerializeField] private string[] layerLabels = new string[] { "主图层", "图层1", "图层2", "图层3" };

    private FactionMember localFaction;
    private SurvivorState localSurvivorState;
    private MapLayer[] chaserLayers = new MapLayer[] { MapLayer.Main, MapLayer.Layer1, MapLayer.Layer2, MapLayer.Layer3 };
    private MapLayer[] currentOptions;
    private bool _subscribedMaskBits;
    private int _lastLoggedOptionCount = -1;

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        for (int i = 0; i < layerButtons.Count; i++)
        {
            int idx = i;
            layerButtons[i].onClick.AddListener(() => OnLayerClicked(idx));
        }
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;
        if (layerMoveAbility == null)
        {
            var po = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (po != null) layerMoveAbility = po.GetComponent<LayerMoveAbility>();
        }
        if (layerMoveAbility == null) return;
        if (localFaction == null) localFaction = layerMoveAbility.GetComponent<FactionMember>();
        if (localSurvivorState == null) localSurvivorState = layerMoveAbility.GetComponent<SurvivorState>();

        if (Input.GetKeyDown(openKey))
        {
            if (layerMoveAbility.CanUse() && panelRoot != null && !panelRoot.activeSelf)
            {
                ShowOptions();
                panelRoot.SetActive(true);
            }
            else if (panelRoot != null && panelRoot.activeSelf)
            {
                panelRoot.SetActive(false);
                ClearUISelection();
                StartCoroutine(ClearUISelectionNextFrame());
            }
        }
        if (localSurvivorState != null && !_subscribedMaskBits)
        {
            localSurvivorState.CarriedDeadMaskBitsChanged += RefreshOptionsIfPanelOpen;
            _subscribedMaskBits = true;
        }
    }

    private void OnDestroy()
    {
        if (localSurvivorState != null && _subscribedMaskBits)
        {
            localSurvivorState.CarriedDeadMaskBitsChanged -= RefreshOptionsIfPanelOpen;
            _subscribedMaskBits = false;
        }
    }

    private void RefreshOptionsIfPanelOpen()
    {
        if (panelRoot != null && panelRoot.activeSelf)
            ShowOptions();
    }

    private void ShowOptions()
    {
        if (localFaction == null || layerButtons.Count < 4) return;
        if (localFaction.IsChaser)
            currentOptions = chaserLayers;
        else if (localFaction.IsSurvivor)
        {
            // 主图层 + 自己的面具图层 + 所有已拾取的死亡面具对应图层（可多个，去重后按 Main/L1/L2/L3 顺序）
            int maskId = localFaction.MaskId;
            var set = new HashSet<MapLayer> { MapLayer.Main, GameLayerSkillConfig.MaskIndexToLayer(maskId) };
            var carriedIndices = localSurvivorState != null ? localSurvivorState.GetCarriedDeadMaskIndices() : new List<int>();
            foreach (int idx in carriedIndices)
                set.Add(GameLayerSkillConfig.MaskIndexToLayer(idx));
            var order = new MapLayer[] { MapLayer.Main, MapLayer.Layer1, MapLayer.Layer2, MapLayer.Layer3 };
            var list = new List<MapLayer>();
            foreach (var layer in order)
                if (set.Contains(layer)) list.Add(layer);
            currentOptions = list.ToArray();
            if (currentOptions.Length != _lastLoggedOptionCount)
            {
                _lastLoggedOptionCount = currentOptions.Length;
                ulong localId = NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null ? NetworkManager.Singleton.LocalClientId : 999;
                GameLogGUI.AddLine($"[J选项] LocalClientId={localId} MaskId={maskId} carriedIndices=[{string.Join(",", carriedIndices)}] 选项数={currentOptions.Length}");
            }
        }
        else
        {
            currentOptions = new MapLayer[0];
            _lastLoggedOptionCount = -1;
        }

        for (int i = 0; i < layerButtons.Count; i++)
        {
            bool show = i < currentOptions.Length;
            layerButtons[i].gameObject.SetActive(show);
            if (show)
            {
                var label = layerButtons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (label != null) label.text = GetLayerLabel(currentOptions[i]);
            }
        }
    }

    private string GetLayerLabel(MapLayer layer)
    {
        switch (layer)
        {
            case MapLayer.Main: return layerLabels.Length > 0 ? layerLabels[0] : "主图层";
            case MapLayer.Layer1: return layerLabels.Length > 1 ? layerLabels[1] : "图层1";
            case MapLayer.Layer2: return layerLabels.Length > 2 ? layerLabels[2] : "图层2";
            case MapLayer.Layer3: return layerLabels.Length > 3 ? layerLabels[3] : "图层3";
            default: return layer.ToString();
        }
    }

    private void OnLayerClicked(int buttonIndex)
    {
        if (layerMoveAbility == null || currentOptions == null) return;
        if (buttonIndex < 0 || buttonIndex >= currentOptions.Length) return;
        layerMoveAbility.RequestMoveToLayer(currentOptions[buttonIndex]);
        if (panelRoot != null) panelRoot.SetActive(false);
        ClearUISelection();
        StartCoroutine(ClearUISelectionNextFrame());
        UIMgr.Get<UIMain>().LayerSkill.UseSkill();
    }

    private void ClearUISelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private System.Collections.IEnumerator ClearUISelectionNextFrame()
    {
        yield return null;
        ClearUISelection();
    }
}
