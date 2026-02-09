using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace Network
{
    public partial class Server
    {
        /// <summary>
        /// 有且只有继承了NetworkBehaviour的类，它的共享字段会生效,
        /// 基础的int,bool之类的可以共享，无法使用引用类型
        /// 如：List<NetworkVariable<int>>  等写法都会导致失效并且不会报错
        /// </summary>
        public NetworkVariable<int> Test = new NetworkVariable<int>();

        /// <summary>
        /// 只会在服务器运行，修改的NetworkVariable的值会异步修改到所有客户端
        /// </summary>
        /// <param name="value">参数也无法使用引用类型，NetworkObjectReference可以通过Id简单获取NetworkObject对象</param>
        [ServerRpc(RequireOwnership = false)]
        public void TestServerRpc(int value, NetworkObjectReference refObj)
        {
            if(refObj.TryGet(out var netObj))
            {
                Test.Value += value;
            }

            // 封装了一个获取组件的方法
            if(GetNetworkObj(refObj, out var com))
            {
                Test.Value += value;
            }
            if(GetNetworkObj<Transform>(refObj, out var com2))
            {
                Test.Value += value;
            }
        }

        /// <summary>
        /// 客户端向客户端发送信息
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        [ServerRpc(RequireOwnership = false)]
        public void TestClient2ClientServerRpc(ulong from, ulong to)
        {
            ClientRpcParams p = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { to }
                },
            };

            // 需要经过服务器转发
            TestNotifyClientRpc( from, p);
        }

        /// <summary>
        /// 客户端方法，让目标客户端执行代码，只能由服务器调用
        /// </summary>
        /// <param name="from"></param>
        /// <param name="p"></param>
        [ClientRpc]
        public void TestNotifyClientRpc(ulong from, ClientRpcParams p)
        {
            // ClientRpcParams需要在参数的最后。无需检查，会自动送到目标客户端执行
            // 除非指定，否则默认的Owner都是服务器，使用NetworkManager.LocalClientId而不是OwnerClientId来获取本机id
            Debug.LogError($"来自{from} 由 {NetworkManager.LocalClientId} 执行, 目标{p.Send.TargetClientIds}");
        }
    }
}
