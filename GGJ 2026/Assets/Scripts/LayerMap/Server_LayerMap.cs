using LayerMap;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace Network
{
    public partial class Server
    {
        /// <summary>
        /// 请求生成客户端数据
        /// </summary>
        /// <param name="clientId">客户端Id</param>
        [ServerRpc(RequireOwnership = false)]
        public void SpanLayerMapClientServerRpc(ulong clientId)
        {
            if (LayerMapManager.Instance.GetClient(clientId) == null)
            {
                var prefab = Resources.Load<LayerMapClient>(typeof(LayerMapClient).Name);
                var go = Instantiate(prefab);
                go.Spawn(clientId);
                go.Name.Value = $"Client{clientId}";
                go.GotoLayer(MapLayer.Main); // 初始当前图层为主图层
                go.transform.SetParent(LayerMapManager.Instance.transform);
            }
            else
            {
                Debug.LogWarning($"[Server] 重复请求 LayerMapClient clientId={clientId}");
            }
        }

        /// <summary>
        /// 请求前往目标图层（修改数据，并通知所有客户端刷新状态）
        /// </summary>
        /// <param name="clientId">客户端Id</param>
        /// <param name="targetLayer">目标图层</param>
        [ServerRpc(RequireOwnership = false)]
        public void GotoMapLayerServerRpc(ulong clientId, MapLayer targetLayer)
        {
            if(LayerMapManager.Instance.TryGetClient(clientId, out var client))
            {
                client.GotoLayer(targetLayer);
            }
            else
            {
                Debug.LogError($"[Server]  \nCan't find Id:{clientId}");
            }
        }
    }
}
