using UnityEngine;

namespace Util
{
    public class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
    {
        private static T s_Instance;
        static bool isExit;

        public static T Instance
        {
            get
            {
                if(isExit)
                {
                    return s_Instance;
                }
                if (s_Instance != null) return s_Instance;

                s_Instance = FindObjectOfType<T>();
                if (s_Instance == null)
                {
                    var go = new GameObject(typeof(T).Name);
                    s_Instance = go.AddComponent<T>();
                }
                s_Instance.OnInit();
                return s_Instance;
            }
        }

        private void OnDestroy()
        {
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