using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.UI;

namespace System
{
    public static class Enum_Ex
    {
        public static string ToName(this Enum value)
        {
            var ret = string.Empty;
            var enumType = value.GetType();
            var filed = enumType.GetField(value.ToString());
            if (filed.IsDefined(typeof(DescriptionAttribute), false))
            {
                var des = (DescriptionAttribute)filed.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
                return des.Description;
            }
            return ret;
        }
    }
}

namespace UnityEngine
{
    public static class GameObject_Ex
    {
        /// <summary>
        /// 尝试获取组件，获取不到时自动添加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <returns></returns>
        public static T TryGetComponent<T>(this GameObject go) where T : Component
        {
            var com = go.GetComponent<T>();
            if(com == null)
            {
                com = go.AddComponent<T>();
            }
            return com;
        }

        public static T FindNode<T>(this GameObject go, string path, bool showError = true) where T : Component
        {
            return go.transform.FindNode<T>(path, showError);
        }

        public static bool ChangeActive(this GameObject go, bool active)
        {
            if (go.activeInHierarchy == active)
            {
                return false;
            }
            go.SetActive(active);
            return true;
        }

    }

    public static class Tranform_Ex
    {
        public static Button BindButton(this Transform ts, string pathOrName, Events.UnityAction action, bool showError = true)
        {
            return ts.FindNode<Button>(pathOrName, showError).BindButton(action);
        }


        public static T FindComponent<T>(this Transform ts, bool showError = true) where T : Component
        {
            var res = ts.GetComponent<T>();
            if (res == null)
            {
                return FindComponentParma<T>(ts.gameObject);
            }
            return res;
        }
        public static List<T> FindAllComponent<T>(this Transform ts, bool showError = true) where T : Component
        {
            List<T> list = new List<T>();
            var res = ts.GetComponent<T>();
            if (res != null)
            {
                list.Add(res);
            }
            FindAllComponentParma(ts.gameObject, list);
            return list;
        }
        public static T FindNode<T>(this Transform ts, string pathOrName, bool showError = true) where T : Component
        {
            if(ts == null)
            {
                return default;
            }
            var t = FindParmaByName(ts.gameObject, pathOrName);
            if (t != null)
            {
                var c = t.GetComponent<T>();
                if(c == null && showError)
                {
                    Debug.LogError($"Node:{pathOrName} not have {nameof(T)}");
                }
                return c;
            }
            t = ts.FindNodeByPath(pathOrName);
            if (t != null)
            {
                var c = t.GetComponent<T>();
                if (c == null && showError)
                {
                    Debug.LogError($"Node:{pathOrName} not have {nameof(T)}");
                }
            }
            if (showError)
            {
                Debug.LogError($"Node:{pathOrName} not find");
            }
            return default;
        }
        /// <summary>
        /// 尝试获取目标节点的组件，目标节点没有对应组件时自动添加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ts"></param>
        /// <param name="pathOrName"></param>
        /// <param name="showError"></param>
        /// <returns></returns>
        public static T TryFindComponent<T>(this Transform ts, string pathOrName, bool showError = true) where T : Component
        {
            var trans = ts.FindNode<Transform>(pathOrName, showError);
            if (trans == null)
            {
                return default;
            }
            var com = trans.GetComponent<T>();
            if(com != null)
            {
                return com;
            }
            com = trans.gameObject.AddComponent<T>();
            return com;
        }
        static Transform FindParmaByName(GameObject child, string name)
        {
            //利用for循环 获取物体下的全部子物体
            for (int c = 0; c < child.transform.childCount; c++)
            {
                //如果子物体下还有子物体 就将子物体传入进行回调查找 直到物体没有子物体为止
                var cGo = child.transform.GetChild(c);
                if (cGo.childCount > 0)
                {
                    var res = FindParmaByName(child.transform.GetChild(c).gameObject, name);
                    if(res != null)
                    {
                        return res;
                    }
                }
                if (cGo.name.Equals(name))
                {
                    //_data.Add(child.transform.GetChild(c).name, child.transform.GetChild(c).transform);
                    return child.transform.GetChild(c).transform;
                }
            }
            return default;
        }
        static T FindComponentParma<T>(GameObject child)
        {
            //利用for循环 获取物体下的全部子物体
            for (int c = 0; c < child.transform.childCount; c++)
            {
                //如果子物体下还有子物体 就将子物体传入进行回调查找 直到物体没有子物体为止
                var cGo = child.transform.GetChild(c);
                if (cGo.childCount > 0)
                {
                    var res = FindComponentParma<T>(child.transform.GetChild(c).gameObject);
                    if(res != null)
                    {
                        return res;
                    }
                }
                var tc = cGo.GetComponent<T>();
                if (tc != null)
                {
                    //_data.Add(child.transform.GetChild(c).name, child.transform.GetChild(c).transform);
                    return tc;
                }
            }
            return default;
        }
        static Transform FindAllComponentParma<T>(GameObject child, List<T> list)
        {
            //利用for循环 获取物体下的全部子物体
            for (int c = 0; c < child.transform.childCount; c++)
            {
                //如果子物体下还有子物体 就将子物体传入进行回调查找 直到物体没有子物体为止
                var cGo = child.transform.GetChild(c);
                if (cGo.childCount > 0)
                {
                    var res = FindAllComponentParma<T>(child.transform.GetChild(c).gameObject, list);
                    if(res != null)
                    {
                        return res;
                    }
                }
                var tc = cGo.GetComponent<T>();
                if (tc != null)
                {
                    list.Add(tc);
                    return cGo;
                }
            }
            return default;
        }
        static Transform FindNodeByPath(this Transform ts, string path)
        {
            var paths = path.Split('/');
            var t = ts;
            foreach (var p in paths)
            {
                t = t.Find(p);
                if (t != null)
                {
                    break;
                }
            }
            if(t == null)
            {
                return null;
            }
            return t;
        }
    }
}

namespace UnityEngine.UI
{
    public static class Button_Ex
    {
        /// <summary>
        /// 绑定按钮点击事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <returns></returns>
        public static Button BindButton(this Button btn, Events.UnityAction action)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
            return btn;
        }
    }

    public static class GameObject_Ex
    {
        /// <summary>
        /// 绑定按钮点击事件，无按钮时自动添加按钮
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <returns></returns>
        public static Button BindButton(this GameObject go, Events.UnityAction action)
        {
            var btn = go.TryGetComponent<Button>();
            btn.BindButton(action);
            return btn;
        }
    }
}
