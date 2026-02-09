using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 服务器端脚本继承此类，并制作成预制件放置在特定位置，可以单独在服务器自动创建这些预制件
    /// </summary>
    public class NetworkServerBase : MonoBehaviour
    {
        public Server Server => NetworkManagerEx.Instance.Server;
        public NetworkManager NetworkManager => NetworkManagerEx.NetworkManager;

        public void Init()
        {
            OnInit();
        }

        public virtual void OnInit()
        {

        }

        public void GameStart()
        {
            OnGameStart();
        }
        public virtual void OnGameStart()
        {

        }
    }
}
