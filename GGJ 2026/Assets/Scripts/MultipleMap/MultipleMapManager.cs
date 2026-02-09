using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network;
using Unity.Netcode;

namespace MultipleMap
{
    public class MultipleMapManager : Util.SingletonNetworkMono<MultipleMapManager>
    {
        /// <summary>
        /// 请求生成客户端的图层记录信息
        /// </summary>
        public void ReqGenClient()
        {
            _server.SpanMultipleMapClientServerRpc(_networkManager.LocalClientId);
        }
        /// <summary>
        /// 请求前往目标图层
        /// </summary>
        public void ReqGotoMapLayer(MapLayer targetLayer, NetworkObjectReference playerRef)
        {
            _server.GotoMultipleMapServerRpc(NetworkManager.LocalClientId, targetLayer, playerRef);
        }
        /// <summary>
        /// 获取指定客户端的图层记录信息
        /// </summary>
        public MultipleMapClient GetClient(ulong clientId)
        {
            return AllClient.Find(x => x.OwnerClientId == clientId);
        }
        /// <summary>
        /// 获取指定客户端的图层记录信息
        /// </summary>
        public bool TryGetClient(ulong clientId, out MultipleMapClient client)
        {
            client = GetClient(clientId);
            return client != null;
        }
        /// <summary>
        /// 当前客户端的图层记录信息
        /// </summary>
        public MultipleMapClient Client => GetClient(_networkManager.LocalClientId);
        List<MultipleMapClient> _allClient = new List<MultipleMapClient>();
        public List<MultipleMapClient> AllClient
        {
            get
            {
                if(_allClient.Count != this.transform.childCount)
                {
                    // 组件数量不对时更新列表
                    _allClient = new List<MultipleMapClient>();
                    for (int i = 0; i < this.transform.childCount; i++)
                    {
                        _allClient.Add(this.transform.GetChild(i).GetComponent<MultipleMapClient>());
                    }
                }
                return _allClient;
            }
        }
        NetworkManager _networkManager => NetworkManagerEx.NetworkManager;
        Server _server => NetworkManagerEx.Instance.Server;
    }
}
