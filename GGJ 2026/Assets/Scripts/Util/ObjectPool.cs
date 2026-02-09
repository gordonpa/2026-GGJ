using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Util
{
    /// <summary>
    /// 对象池，按照对象名字存储，请确保名字相同的对象唯一
    /// </summary>
    public class ObjectPool : SingletonMono<ObjectPool>
    {
        public readonly string PrefabPath = "";
        /// <summary>
        /// 池子的最大容量
        /// </summary>
        public int MaxCount => 100;
        public int _curCount = 0;
        /// <summary>
        /// 池子的当前容量
        /// </summary>
        public int CurCount => _curCount;

        private Dictionary<string, Queue<GameObject>> _pool = new();

        private void Awake()
        {
            for (int i = 0; i < this.transform.childCount; i++)
            {
                ReturnObj(this.transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 获取的对象不存在时，将从Resources的预制件文件夹查找对象。查找不到时返回null
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public GameObject GetObj(string name)
        {
            var res = TryGetObj(name);
            if (res != null)
            {
                InitObj(res);
                return res;
            }
            res = LoadObject(name);
            InitObj(res);
            return res;
        }

        private void InitObj(GameObject obj)
        {
        }

        /// <summary>
        /// 获取的对象不存在时，将从Resources的预制件文件夹查找对象。查找不到时返回null
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetObj<T>(string name) where T : Component
        {
            var res = GetObj(name);
            return res.GetComponent<T>();
        }

        /// <summary>
        /// 获取的对象不存在时，将从Resources的预制件文件夹查找对象。查找不到时返回null
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetObj<T>() where T : Component
        {
            var res = GetObj(typeof(T).Name);
            return res.GetComponent<T>();
        }

        /// <summary>
        /// 获取的对象不存在时，返回参数对象的复制
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public GameObject GetObj(GameObject go)
        {
            var res = TryGetObj(go.name);
            if (res == null)
            {
                return Instantiate(go);
            }
            return res;
        }

        /// <summary>
        /// 获取的对象不存在时，返回null
        /// </summary>
        /// <returns></returns>
        private GameObject TryGetObj(string name)
        {
            name = name.Replace("(Clone)", "");
            if (_pool.TryGetValue(name, out var res))
            {
                if (res.Count > 0)
                {
                    _curCount--;
                    return res.Dequeue();
                }
                else if (res.Count == 1)
                {
                    _curCount--;
                    _pool.Remove(name);
                    return res.Dequeue();
                }
                else
                {
                    _pool.Remove(name);
                }
            }
            return null;
        }

        /// <summary>
        /// 加载游戏对象，从Resources的预制件文件夹查找对象。查找不到时返回null
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private GameObject LoadObject(string name)
        {
            var prefab = Resources.Load<GameObject>($"{PrefabPath}{name}");
            GameObject res = null;
            if (prefab != null)
            {
                res = GameObject.Instantiate(prefab);
            }
            return res;
        }


        public void ReturnAllChild(GameObject obj)
        {
            ReturnAllChild(obj.transform);
        }
        public void ReturnAllChild(Transform obj)
        {
            while(obj.childCount > 0)
            {
                ReturnObj(obj.GetChild(0).gameObject);
            }
        }

        /// <summary>
        /// 将对象返回对象池，超过对象池最大数量时，对象将会被销毁
        /// </summary>
        /// <param name="obj"></param>
        public void ReturnObj(GameObject obj)
        {
            if(obj == null)
            {
                return;
            }

            obj.gameObject.ChangeActive(false);
            obj.transform.SetParent(this.transform);

            var name = obj.name.Replace("(Clone)","");

            _curCount++;

            if (_curCount > MaxCount)
            {
                Destroy(obj);
            }
            else
            {
                if (_pool.TryGetValue(name, out var res))
                {
                    if(res.Contains(obj))
                    {
                        _curCount--;
                    }
                    else
                    {
                        res.Enqueue(obj);
                    }
                }
                else
                {
                    var q = new Queue<GameObject>();
                    q.Enqueue(obj);
                    _pool.Add(name, q);
                }
            }
            _curCount = Mathf.Clamp(_curCount, 0, MaxCount);
        }

        /// <summary>
        /// 销毁所有对象，清除引用
        /// </summary>
        public void Clear()
        {
            foreach (var q in _pool.Values)
            {
                foreach (var go in q)
                {
                    Destroy(go);
                }
            }
            _pool.Clear();
        }
    }
}
