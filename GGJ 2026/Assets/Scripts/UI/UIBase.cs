using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public enum UILayerType
{
    Normol = 100,
    System = 500,
}
public class UILayer
{
    public UILayerType Type;
    public static int GetLayer(UILayerType type)
    {
        switch (type)
        {
            case UILayerType.Normol:
                return Normol++;
            case UILayerType.System:
                return System++;
            default:
                return Normol++;
        }
    }
    public static int ReturnLayer(UILayerType type)
    {
        switch (type)
        {
            case UILayerType.Normol:
                return Normol--;
            case UILayerType.System:
                return System--;
            default:
                return Normol--;
        }
    }
    /// <summary>
    /// 系统级别的UI，在最前方显示
    /// </summary>
    public static int System = 500;
    /// <summary>
    /// 普通UI
    /// </summary>
    public static int Normol = 100;
}

public class UIBase : MonoBehaviour, IUIPanel
{
    /// <summary>
    /// 在逻辑上是否显示
    /// </summary>
    protected bool _isLogicalShow;
    /// <summary>
    /// 在逻辑上是否显示
    /// </summary>
    public bool IsLogicalShow => _isLogicalShow;
    /// <summary>
    /// 是否为基岩页面（无法被Back关闭，Back将会尝试开始使用循环页面）
    /// </summary>
    public bool IsBedrock;
    /// <summary>
    /// 该UI的推荐Layer
    /// </summary>
    public virtual UILayerType Layer => UILayerType.Normol;

    public void Init()
    {
        OnInit();
    }

    protected virtual void OnInit() { }

    /// <summary>
    /// 显示，负责UI的显示流程，包括重复显示处理，显示动画处理，数据显示等
    /// </summary>
    public void Show(bool hideAni = false, bool force = false)
    {
        if (_isLogicalShow)
        {
            Debug.LogWarning($"{this.name}重复显示");
            return;
        }
        _isLogicalShow = true;

#if UNITY_EDITOR
        var time = Time.realtimeSinceStartup;
        // 刷新数据
        OnShowBefore();
#else
        try
        {
            // 刷新数据
            OnShowBefore();
        }
        catch(System.Exception e)
        {
            Debug.LogError(e);
        }
#endif

        if (hideAni)
        {
            this.gameObject.ChangeActive(true);
        }
        else
        {
            // 可能的动画处理，用协程与数据刷新同步进行

            this.gameObject.ChangeActive(true);
        }
#if UNITY_EDITOR
        OnShowAfter();
#else
        try
        {
            // 刷新数据
            OnShowAfter();
        }
        catch(System.Exception e)
        {
            Debug.LogError(e);
        }
#endif
    }

    /// <summary>
    /// 在显示前刷新UI数据
    /// </summary>
    public virtual void OnShowBefore() { }
    /// <summary>
    /// 在显示后刷新UI数据
    /// </summary>
    public virtual void OnShowAfter() { }

    public void Hide(bool hideAni = false, bool force = false, bool init = false)
    {
        if (!_isLogicalShow && !force)
        {
            Debug.LogWarning($"{this.name}: 重复关闭");
            return;
        }
        _isLogicalShow = false;

        if (hideAni)
        {
            try
            {
                OnHide();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
            this.gameObject.ChangeActive(false);
        }
        else
        {
            // 可能的动画处理，动画处理完毕后隐藏

            OnHide();
            this.gameObject.ChangeActive(false);
        }
        UILayer.ReturnLayer(Layer);
        IsBedrock = false;
    }

    public virtual void OnHide() { }
}
