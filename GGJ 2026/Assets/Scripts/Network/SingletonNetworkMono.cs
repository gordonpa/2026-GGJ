using UnityEngine;
using Unity.Netcode;
using Network;

namespace Util
{
    public class SingletonNetworkMono<T> : NetworkBehaviour where T : SingletonNetworkMono<T>
    {
        private static T s_Instance;
        static bool isExit;

        public static T Instance
        {
            get
            {
                if (isExit)
                {
                    return s_Instance;
                }
                if (s_Instance != null) return s_Instance;

                s_Instance = FindObjectOfType<T>();
                if (s_Instance == null)
                {
                    GameObject go = null;
                    var prefab = Resources.Load<NetworkObject>(typeof(T).Name);
                    go = Instantiate(prefab).gameObject;
                    s_Instance = go.GetComponent<T>();
                }
                s_Instance.OnInit();
                return s_Instance;
            }
        }



        public override void OnDestroy()
        {
            base.OnDestroy();
            isExit = true;
        }

        public virtual void OnInit() { }

        public static void DestroyInstance()
        {
            DestroyImmediate(s_Instance);
            s_Instance = null;
        }
    }
}