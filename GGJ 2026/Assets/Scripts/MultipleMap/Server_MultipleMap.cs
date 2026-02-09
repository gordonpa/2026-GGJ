using MultipleMap;
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
        public void SpanMultipleMapClientServerRpc(ulong clientId)
        {
            Debug.LogError($"[Server] {clientId}:SpanLayerMapClientServerRpc");
            if (MultipleMapManager.Instance.GetClient(clientId) == null)
            {
                var prefab = Resources.Load<MultipleMapClient>(typeof(MultipleMapClient).Name);
                var go = Instantiate(prefab);
                go.Spawn(clientId);
                go.gameObject.name = $"Client{clientId}";
                go.transform.SetParent(MultipleMapManager.Instance.transform);
            }
            else
            {
                Debug.LogError($"[Server] Can't duplicate generation {nameof(MultipleMapClient)}");
            }
        }

        /// <summary>
        /// 请求前往目标图层
        /// </summary>
        /// <param name="clientId">客户端Id</param>
        /// <param name="targetLayer">目标图层</param>
        [ServerRpc(RequireOwnership = false)]
        public void GotoMultipleMapServerRpc(ulong clientId, MapLayer targetLayer, NetworkObjectReference playerRef)
        {
            if (MultipleMapManager.Instance.TryGetClient(clientId, out var client))
            {
                if (GetNetworkObj(playerRef, out var player))
                {
                    // TODO: 将玩家瞬间移动后再重新启动插值。或者不用NetworkTransform组件自行实现插值
                    player.transform.position = player.transform.position + Vector3.up * ((int)targetLayer - (int)client.Layer.Value) * 1000;
                }
            }
            else
            {
                Debug.LogError($"[Server]  \nCan't find Id:{clientId}");
            }
        }
    }
}
