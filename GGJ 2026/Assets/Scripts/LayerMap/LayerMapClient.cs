using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
namespace LayerMap
{
    public enum MapLayer
    {
        None = 0,
        /// <summary>
        /// 主图层
        /// </summary>
        Main = 1,
        /// <summary>
        /// 图层1
        /// </summary>
        Layer1 = 2,
        /// <summary>
        /// 图层2
        /// </summary>
        Layer2 = 3,
        /// <summary>
        /// 图层3
        /// </summary>
        Layer3 = 4,
    }

    /// <summary>
    /// 相当于每个客户端独立的存档数据
    /// 使用这个的话，可能需要自行通过存档数据同步场景
    /// 也就是数据及时同步，场景根据各自客户端需要构建运行
    /// </summary>
    [RequireComponent(typeof(LayerMapSync))]
    public class LayerMapClient : Network.NetworkBase
    {
        /// <summary>
        /// 本机的分数
        /// </summary>
        public NetworkVariable<int> Score = new NetworkVariable<int>();
        public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>();

        private FactionMember localFaction;
        /// <summary>
        /// 本机的阵营
        /// </summary>
        public FactionMember FactionMember
        {
            get
            {
                var po = NetworkManager.Singleton.LocalClient?.PlayerObject;
                if (po == null) return localFaction;
                if (localFaction == null) localFaction = po.GetComponent<FactionMember>();
                return localFaction;
            }
        }


        /// <summary>
        /// 当前客户端的地图层级：主地图为0，依次递增
        /// </summary>
        public NetworkVariable<MapLayer> Layer = new NetworkVariable<MapLayer>();
        public NetworkVariable<FixedString64Bytes> Name = new NetworkVariable<FixedString64Bytes>();

        public LayerMapSync SyncControl;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            this.name = Name.Value.ToString();

            Server.SynTime();
            Server.Timestamp.OnValueChanged += Server.OnTimeChange;
            SyncControl = GetComponent<LayerMapSync>();
            Layer.OnValueChanged += OnRefreshLayerStatus;
            Name.OnValueChanged += OnRefreshName;

            // 服务器与客户端：都把 LayerMapClient 挂到 LayerMapManager 下，这样服务器 TryInteractServerRpc 里 TryGetClient(clientId) 能拿到任意玩家的当前图层，客户端 GetClient 也能用于显隐
            if (LayerMapManager.Instance != null)
            {
                transform.SetParent(LayerMapManager.Instance.transform, worldPositionStays: false);
            }
            // 本地玩家：同步当前图层到 LayerTaskManager，使 LayerCollectible/LayerSubmitZone 的 layerId 判定与图层一致（不修改它们内部代码）
            if (IsClient && NetworkManager.Singleton != null && OwnerClientId == NetworkManager.Singleton.LocalClientId && LayerTaskManager.Instance != null)
                LayerTaskManager.SetCurrentLayer(GameLayerSkillConfig.MapLayerToLayerId(Layer.Value));

            Debug.Log($"[LayerMapClient],listening start OwnerClientId=" + OwnerClientId);
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Layer.OnValueChanged -= OnRefreshLayerStatus;
            Name.OnValueChanged -= OnRefreshName;
            Server.Timestamp.OnValueChanged -= Server.OnTimeChange;
            Debug.Log($"[LayerMapClient],listening end");
        }

        private void OnRefreshLayerStatus(MapLayer oldValue, MapLayer newValue)
        {
            bool isLocal = IsClient && NetworkManager.Singleton != null && OwnerClientId == NetworkManager.Singleton.LocalClientId;
            GameLogGUI.AddLine($"[图层] ClientId={OwnerClientId} {(isLocal ? "本地" : "")} {oldValue}→{newValue}");
            Debug.Log($"[LayerMapClient] ClientId:{OwnerClientId} OnRefreshLayerStatus:{oldValue} :{newValue}");
            RefreshLayerStatus();
            // 本地玩家：J 切换图层后同步到 LayerTaskManager，使任务物品/提交点 layerId 判定正确
            if (IsClient && NetworkManager.Singleton != null && OwnerClientId == NetworkManager.Singleton.LocalClientId && LayerTaskManager.Instance != null)
            {
                int layerId = GameLayerSkillConfig.MapLayerToLayerId(newValue);
                LayerTaskManager.SetCurrentLayer(layerId);
                GameLogGUI.AddLine($"[图层] LayerTaskManager.SetCurrentLayer({layerId})");
            }
        }
        private void OnRefreshName(FixedString64Bytes oldValue, FixedString64Bytes newValue)
        {
            Debug.Log($"[LayerMapClient] ClientId:{OwnerClientId} OnRefreshLayerStatus:{oldValue} :{newValue}");
            this.name = Name.Value.ToString();
        }

        public void GotoLayer(MapLayer targetLayer)
        {
            Layer.Value = targetLayer;
        }
        /// <summary>
        /// 根据存档数据刷新场景
        /// </summary>
        public void RefreshLayerStatus()
        {
            SyncControl.RefreshLayerStatus();
        }
    }
}
