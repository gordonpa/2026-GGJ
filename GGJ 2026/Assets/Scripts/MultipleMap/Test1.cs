using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
namespace MultipleMap
{
    public class Test1 : MonoBehaviour
    {
        public Transform Player;
        NetworkObjectReference _Player;
        [InspectorButton("主机")]
        public void TestHost()
        {
            Network.NetworkManagerEx.Instance.StartHost();
            // 需要等链接上服务器后再请求
            Invoke(nameof(Wait), 1f);
        }
        [InspectorButton("客户端")]
        public void TestClient()
        {
            Network.NetworkManagerEx.Instance.StartClient();
            // 需要等链接上服务器后再请求
            Invoke(nameof(Wait2), 1f );

        }

        void Wait()
        {
            MultipleMapManager.Instance.ReqGenClient();
            Invoke(nameof(Wait2), 1f);
            var netObj = Instantiate(Player).GetComponent<NetworkObject>();
            netObj.gameObject.ChangeActive(true);
            netObj.Spawn();
            netObj.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
            _Player = new NetworkObjectReference(netObj);
            MultipleMapManager.Instance.ReqGotoMapLayer(TargetLayer, _Player);
        }
        void Wait2()
        {
            MultipleMapManager.Instance.ReqGenClient();
            Invoke(nameof(Wait3), 1f);
        }
        void Wait3()
        {
            MultipleMapManager.Instance.ReqGotoMapLayer(TargetLayer, _Player);
        }

        public MapLayer TargetLayer;
        [InspectorButton("前往目标图层")]
        public void GotoLayer()
        {
            MultipleMapManager.Instance.ReqGotoMapLayer(TargetLayer, _Player);
        }
    }
}
