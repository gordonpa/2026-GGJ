using UnityEngine;

/// <summary>
/// 场景一加载就显示 UIMain（主界面）。挂到有 UICfg 的物体或任意会先运行的物体上即可。
/// </summary>
public class ShowUIMainOnLoad : MonoBehaviour
{
    [Tooltip("是否在 Start 时自动打开 UIMain")]
    [SerializeField] private bool showOnStart = true;

    private void Start()
    {
        if (showOnStart)
            UIMgr.Change<UIMain>();
    }
}
