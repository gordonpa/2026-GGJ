using System.Collections.Generic;
using UnityEngine;

public class UIMgr : Util.Singleton<UIMgr>
{
    /// <summary>
    /// 存储所有UI
    /// </summary>
    public static readonly List<UIBase> Panels = new();
    static Stack<UIBase> CurPanel = new Stack<UIBase>();

    static public void Enqueue(UIBase ui)
    {
        CurPanel.Push(ui);
    }

    static public UIBase Dequeue()
    {
        return CurPanel.Pop();
    }

    static public void Back()
    {
        if(CurPanel.Peek().IsBedrock)
        {
            var loopPanel = UICfg.Instance.LoopPanel;
            if (loopPanel != null)
            {
                // 进入循环页面
                if (loopPanel.IsLogicalShow)
                {
                    loopPanel.Hide();
                }
                else
                {
                    loopPanel.Show();
                }
            }
            // 没有循环页面，不做反应
        }
        else
        {
            Dequeue().Hide();
        }
    }

    static public void CloseAllUI()
    {
        while (CurPanel.Count > 0)
        {
            var ui = CurPanel.Peek();
            if (ui.IsLogicalShow)
            {
                ui.Hide(true, true);
            }
        }
    }

    public static T Get<T>() where T : UIBase, IUIPanel, new()
    {
        // 查找或创建实例
        var panel = Panels.Find(x => x is T);
        if (panel == null)
        {
            panel = UICfg.Instance.LoadUI<T>();
            if (panel == null)
            {
                Debug.LogError("加载UI出错");
                return null;
            }
            // 创建UI并存储
            Panels.Add(panel);
            // 创建实例时初始化
            panel.Init();
            panel.Hide(true, true, true);
        }

        return (T)panel;
    }
    /// <summary>
    /// 更改当前显示的页面（关闭所有其他页面，前往目标页面）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="hideAni"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    public static T Change<T>(bool hideAni = false, bool force = false) where T : UIBase, IUIPanel, new()
    {
        // 查找或创建实例
        var panel = Get<T>();

        if (panel != null && !panel.IsLogicalShow)
        {
            CloseAllUI();
            Enqueue(panel);
            panel.IsBedrock = true;
            panel.Show(hideAni, force);
        }
        return panel;
    }
    /// <summary>
    /// 设置循环页面
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="hideAni"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    public static T SetLoopPanel<T>() where T : UIBase, IUIPanel, new()
    {
        // 查找或创建实例
        var panel = Get<T>();

        if (panel != null)
        {
            UICfg.Instance.LoopPanel = panel;
        }
        return panel;
    }
    /// <summary>
    /// 在当前页面上方叠加显示
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="hideAni"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    public static T Add<T>(bool hideAni = false, bool force = false) where T : UIBase, IUIPanel, new()
    {
        // 查找或创建实例
        var panel = Get<T>();

        if (panel != null && !panel.IsLogicalShow)
        {
            Enqueue(panel);
            panel.Show(hideAni, force);
        }
        return panel;
    }
    /// <summary>
    /// 显示页面，该代码控制的页面将不受Change，Add的控制
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="hideAni"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    public static T Show<T>(bool hideAni = false, bool force = false) where T : UIBase, IUIPanel, new()
    {
        // 查找或创建实例
        var panel = Get<T>();

        if (panel != null)
        {
            panel.Show(hideAni, force);
        }
        return panel;
    }


    public static T ShowOrHide<T>(bool hideAni = false, bool force = false) where T : UIBase, IUIPanel, new()
    {
        // 查找或创建实例
        var panel = Get<T>();

        if (panel != null)
        {
            if (panel.IsLogicalShow)
            {
                Hide<T>(hideAni, force);
            }
            else
            {
                Show<T>(hideAni, force);
            }
        }
        return panel;
    }


    public static T Hide<T>(bool hideAni = false, bool force = false) where T : UIBase, IUIPanel, new()
    {
        var panel = Panels.Find(x => x is T);
        if (panel == null)
        {
            return (T)panel;
        }
        return (T)panel;
    }

    public static void Clear()
    {
        foreach (var panel in Panels)
        {
            if (panel is MonoBehaviour p)
            {
                UnityEngine.Object.Destroy(p);
            }
        }
        Panels.Clear();
    }
}
