using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace LayerMap
{
    /// <summary>
    /// 自动寻找LayerSign组件，并根据客户端当前的Layer自动显隐
    /// </summary>
    public class LayerMapSync : MonoBehaviour
    {
        LayerSign[] _layerSignList;
        LayerMapClient _client => LayerMapManager.Instance.Client;
        private void Awake()
        {
            RefreshLayerSignList();
        }

        public void RefreshLayerSignList()
        {
            _layerSignList = FindObjectsOfType<LayerSign>();
            GameLogGUI.AddLine($"[LayerSign] RefreshLayerSignList 共 {_layerSignList?.Length ?? 0} 个");
        }

        public void AddLayerSign(LayerSign layerSign)
        {

        }

        public void RefreshLayerStatus()
        {
            if (_client == null)
            {
                GameLogGUI.AddWarning("[LayerSign] RefreshLayerStatus 跳过: Client=null");
                return;
            }
            MapLayer cur = _client.Layer.Value;
            int showCount = 0, hideCount = 0;
            foreach (var obj in _layerSignList)
            {
                if (!obj.Follow)
                {
                    bool show = _client.Layer.Value.Equals(obj.Layer);
                    obj.ShowOrHide(show);
                    if (show) showCount++; else hideCount++;
                }
                else
                {
                    obj.ShowOrHide(true);
                    showCount++;
                }
            }
            GameLogGUI.AddLine($"[LayerSign] Refresh 当前={cur} 显示={showCount} 隐藏={hideCount} 共={_layerSignList?.Length ?? 0}");
        }
    }
}
