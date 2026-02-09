using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICfg : Util.SingletonMono<UICfg>
{
    public readonly string Path = "UI";
    /// <summary>
    /// 循环页面，在到达基岩页面后，返回会循环显示和关闭该页面。用来显示ESC页面
    /// </summary>
    public UIBase LoopPanel;

    public GameObject Root;

    private void Awake()
    {
        Root = this.gameObject;
    }

    private void SetSetting<T>(T t, GameObject go) where T : UIBase, IUIPanel
    {
        go.transform.SetParent(Root.transform);
        SetLayers(t, UILayer.GetLayer(t.Layer));
        var canvas = t.GetComponent<CanvasScaler>();
        if (canvas == null)
        {
            Debug.LogError("UI组件丢失");
            return;
        }
        canvas.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    }

    public void SetLayers<T>(T t, int layer = -1) where T : UIBase, IUIPanel
    {
        var canvas = t.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = t.gameObject.AddComponent<Canvas>();
        }
        canvas.sortingOrder = layer;
    }
    public T LoadUI<T>() where T : UIBase, IUIPanel
    {
        GameObject prefab = null;
        prefab = Resources.Load<GameObject>($"UI/{typeof(T).Name}");
        if (prefab == null)
        {
            Debug.LogError($"UI:{typeof(T).Name} 未加载成功");
            return default;
        }
        var go = GameObject.Instantiate(prefab);
        var com = go.GetComponent(typeof(T).Name);
        if (com == null)
        {
            com = go.AddComponent<T>();
        }
        if (com is T res)
        {
            SetSetting(res, go);
            return res;
        }
        else
        {
            return null;
        }
    }
}
