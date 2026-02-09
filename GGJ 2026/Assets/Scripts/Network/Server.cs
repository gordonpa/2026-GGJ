using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Collections;

namespace Network
{
    public partial class Server : NetworkBehaviour
    {
        NetworkManager _networkManager => NetworkManager.Singleton;


        /// <summary>
        /// 只会在服务器运行，修改的NetworkVariable的值会异步修改到所有客户端
        /// </summary>
        /// <param name="value">参数也无法使用引用类型，NetworkObjectReference可以通过Id简单获取NetworkObject对象</param>
        [ServerRpc(RequireOwnership = false)]
        public void Test1ServerRpc(ulong client)
        {
            Debug.Log("测试:" + client);
        }
        public T GetNetworkObj<T>(ulong id)
        {
            if(NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out var res))
            {
                return res.GetComponent<T>();
            }
            return default(T);
        }
        public bool GetNetworkObj<T>(ulong id, out T com)
        {
            com = GetNetworkObj<T>(id);
            return com != null;
        }
        public bool GetNetworkObj<T>(NetworkObjectReference id, out T com)
        {
            com = GetNetworkObj<T>(id.NetworkObjectId);
            return com != null;
        }
        public bool GetNetworkObj(NetworkObjectReference id, out NetworkObject com)
        {
            com = GetNetworkObj<NetworkObject>(id.NetworkObjectId);
            return com != null;
        }
    }
}
