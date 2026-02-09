using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace MultipleMap
{
    public enum MapLayer
    {
        None = 0,
        Main = 1,
        Layer1 = 2,
        Layer2 = 3,
        Layer3 = 4,
    }

    /// <summary>
    /// 相当于每个客户端独立的存档数据
    /// 使用这个的话，可能需要自行通过存档数据同步场景
    /// 也就是数据及时同步，场景根据各自客户端需要构建运行
    /// </summary>
    public class MultipleMapClient : Network.NetworkBase
    {
        /// <summary>
        /// 当前客户端的地图层级：主地图为0，依次递增
        /// </summary>
        public NetworkVariable<MapLayer> Layer = new NetworkVariable<MapLayer>();

        public Action<MapLayer> OnGotoLayer;


        private void Awake()
        {
            Layer.OnValueChanged += OnRefreshLayerStatus;
            Debug.Log($"[LayerMapClient],listening start");
        }
        public override void OnDestroy()
        {
            Layer.OnValueChanged -= OnRefreshLayerStatus;
            Debug.Log($"[LayerMapClient],listening end");
        }
        private void OnRefreshLayerStatus(MapLayer oldValue, MapLayer newValue)
        {
            Debug.Log($"[LayerMapClient] ClientId:{OwnerClientId} OnRefreshLayerStatus:{oldValue} :{newValue}");
            OnGotoLayer?.Invoke(newValue);
        }
    }
}
