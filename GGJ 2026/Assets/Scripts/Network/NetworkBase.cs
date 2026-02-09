using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 用此类代替NetworkBehaviour继承，可以进行一些自定义的统一操作
    /// PS： 取名困难症，这些类名啊，字段名啊可以看看取个好听的名字。求求了。
    /// </summary>
    public class NetworkBase : NetworkBehaviour
    {
        public Server Server => NetworkManagerEx.Instance.Server;
        public NetworkObject NetObj => GetComponent<NetworkObject>();

        public void Spawn()
        {
            NetObj.Spawn();
        }
        public void Spawn(ulong id)
        {
            NetObj.Spawn();
            NetObj.ChangeOwnership(id);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }
    }
}
