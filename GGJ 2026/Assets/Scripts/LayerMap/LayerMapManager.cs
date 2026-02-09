using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network;
using Unity.Netcode;

namespace LayerMap
{
    public class LayerMapManager : Util.SingletonNetworkMono<LayerMapManager>
    {
        /// <summary>
        /// 请求生成客户端的图层记录信息
        /// </summary>
        public void ReqGenClient()
        {
            _server.SpanLayerMapClientServerRpc(_networkManager.LocalClientId);
        }
        /// <summary>
        /// 请求前往目标图层
        /// </summary>
        public void ReqGotoMapLayer(MapLayer targetLayer)
        {
            _server.GotoMapLayerServerRpc(NetworkManager.LocalClientId, targetLayer);
        }
        /// <summary>
        /// 获取指定客户端的图层记录信息
        /// </summary>
        public LayerMapClient GetClient(ulong clientId)
        {
            return AllClient.Find(x => x.OwnerClientId == clientId);
        }
        /// <summary>
        /// 获取指定客户端的图层记录信息
        /// </summary>
        public bool TryGetClient(ulong clientId, out LayerMapClient client)
        {
            client = GetClient(clientId);
            return client != null;
        }
        /// <summary>
        /// 当前客户端的图层记录信息
        /// </summary>
        public LayerMapClient Client => GetClient(_networkManager.LocalClientId);
        List<LayerMapClient> _allClient = new List<LayerMapClient>();
        int _allClientChildCount = -1;

        public List<LayerMapClient> AllClient
        {
            get
            {
                int n = this.transform.childCount;
                if (_allClient.Count != n || _allClientChildCount != n)
                {
                    _allClientChildCount = n;
                    _allClient = new List<LayerMapClient>();
                    for (int i = 0; i < n; i++)
                    {
                        var c = this.transform.GetChild(i).GetComponent<LayerMapClient>();
                        if (c != null) _allClient.Add(c);
                    }
                }
                return _allClient;
            }
        }
        NetworkManager _networkManager => NetworkManagerEx.NetworkManager;
        Server _server => NetworkManagerEx.Instance.Server;
    }
}
