using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LayerMap
{
    public class Test : MonoBehaviour
    {
        public Transform TestP;
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
            Invoke(nameof(Wait), 1f );

        }
        void Wait()
        {
            LayerMapManager.Instance.ReqGenClient();
            LayerMapManager.Instance.ReqGotoMapLayer(TargetLayer);
        }

        public MapLayer TargetLayer;
        [InspectorButton("前往目标图层")]
        public void GotoLayer()
        {
            LayerMapManager.Instance.ReqGotoMapLayer(TargetLayer);
        }
    }
}
