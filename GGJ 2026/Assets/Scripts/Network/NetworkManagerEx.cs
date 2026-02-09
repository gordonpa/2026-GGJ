using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Network
{
    public class NetworkManagerEx : Util.SingletonMono<NetworkManagerEx>
    {
        public static NetworkManager NetworkManager => NetworkManager.Singleton;

        /// <summary>服务器上的 Server 对象（Spawn 后）；客户端从 SpawnedObjects 中取到复制的 Server。</summary>
        Server _serverCached;

        /// <summary>供 LayerMapManager 等调用 ServerRpc 用。必须在服务器 Spawn 后 / 客户端复制到后才非 null。</summary>
        public Server Server => GetOrFindServer();

        Transform _nodeServer;

        private void Start()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null)
            {
                nm.OnClientStarted += OnClientStarted;
                nm.OnServerStarted += OnServerStarted;
                nm.OnClientConnectedCallback += OnClientConnected;
            }
        }

        private void OnDestroy()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null)
            {
                nm.OnClientStarted -= OnClientStarted;
                nm.OnServerStarted -= OnServerStarted;
                nm.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        void OnServerStarted()
        {
            SpawnServerObject();
            GenServer();
            Server.SynTimestampServerRpc();
        }

        bool clientReady;
        bool clientInited;

        void OnClientStarted()
        {
            clientReady = false;
            clientInited = false;
        }
        

        void OnClientReady()
        {
            UIMgr.Change<UIMain>();
            UIMgr.Add<UINameInput>();
        }

        private void Update()
        {
            if(NetworkManager.Singleton != null && LayerMap.LayerMapManager.Instance.Client != null)
            {
                clientReady = true;
            }
            if(!clientInited && clientReady)
            {
                clientInited = true;
                OnClientReady();
            }
        }

        void OnClientConnected(ulong _)
        {
            _serverCached = null;
        }

        /// <summary>在服务器上 Spawn Server 预制体，否则 Client 发的 ServerRpc 无人接收。</summary>
        void SpawnServerObject()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer) return;
            if (_serverCached != null && _serverCached.IsSpawned) return;

            var name = typeof(Server).Name;
            var goPrefab = Resources.Load<GameObject>(name);
            if (goPrefab == null)
            {
                var serverComp = Resources.Load<Server>(name);
                if (serverComp != null) goPrefab = serverComp.gameObject;
            }
            if (goPrefab == null)
            {
                Debug.LogError("[NetworkManagerEx] Resources 中未找到 Server 预制体！");
                return;
            }
            var no = goPrefab.GetComponent<NetworkObject>();
            if (no == null)
            {
                Debug.LogError("[NetworkManagerEx] Server 预制体上无 NetworkObject！");
                return;
            }
            var go = Instantiate(goPrefab);
            no = go.GetComponent<NetworkObject>();
            no.Spawn();
            _serverCached = go.GetComponent<Server>();
            DontDestroyOnLoad(go);
            Debug.Log("[NetworkManagerEx] Server 已 Spawn，ServerRpc 可被接收");
        }

        Server GetOrFindServer()
        {
            if (_serverCached != null && _serverCached.IsSpawned) return _serverCached;
            var nm = NetworkManager.Singleton;
            if (nm == null || nm.SpawnManager == null) return null;
            foreach (var kv in nm.SpawnManager.SpawnedObjects)
            {
                var s = kv.Value.GetComponent<Server>();
                if (s != null) { _serverCached = s; return s; }
            }
            return null;
        }

        void GenServer()
        {
            if (_nodeServer == null)
            {
                _nodeServer = new GameObject("ServerNode").transform;
                DontDestroyOnLoad(_nodeServer.gameObject);
            }
            var prefabList = Resources.LoadAll<Transform>("ServerPrefab");
            foreach (var prefab in prefabList)
            {
                var t = Instantiate(prefab);
                t.SetParent(_nodeServer);
                var server = t.GetComponent<NetworkServerBase>();
                if (server != null)
                    server.Init();
            }
        }

        public void StartHost()
        {
            NetworkManager.StartHost();
            GenServer();
        }

        public void StartClient()
        {
            NetworkManager.StartClient();
        }

        public void Shutdown()
        {
            _serverCached = null;
            NetworkManager.Shutdown();
            if (_nodeServer != null)
            {
                for (int i = _nodeServer.childCount - 1; i >= 0; i--)
                    Destroy(_nodeServer.GetChild(i).gameObject);
            }
        }
    }
}
