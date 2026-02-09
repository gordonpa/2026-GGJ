using UnityEngine;
using Unity.Netcode;
using System.Text;

/// <summary>
/// GUI 上显示本地玩家对象（PlayerObject）的子物体信息，便于对比 Host 与 Client 差异（如 item visual 子物体）。
/// 挂到场景任意物体即可。
/// </summary>
public class PlayerChildrenDebugUI : MonoBehaviour
{
    [Header("显示位置（屏幕比例 0-1）")]
    [SerializeField] private Vector2 positionPercent = new Vector2(0.02f, 0.60f);
    [SerializeField] private Vector2 sizePercent = new Vector2(0.38f, 0.22f);

    [Header("样式")]
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.08f, 0.15f, 0.9f);
    [SerializeField] private Color textColor = Color.white;

    private GUIStyle _boxStyle;
    private GUIStyle _labelStyle;
    private bool _stylesInit;
    private string _logText = "";
    private const int MaxChildNames = 8;

    private void OnGUI()
    {
        InitStyles();
        RefreshLog();
        float x = Screen.width * positionPercent.x;
        float y = Screen.height * positionPercent.y;
        float w = Screen.width * sizePercent.x;
        float h = Screen.height * sizePercent.y;
        GUI.Box(new Rect(x, y, w, h), "", _boxStyle);
        _labelStyle.normal.textColor = textColor;
        _labelStyle.fontSize = Mathf.RoundToInt(13 * (Screen.height / 1080f));
        _labelStyle.wordWrap = true;
        GUI.Label(new Rect(x + 6, y + 6, w - 12, h - 12), _logText, _labelStyle);
    }

    private void RefreshLog()
    {
        var sb = new StringBuilder();
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            _logText = "Player子物体调试\nNetworkManager=null";
            return;
        }
        bool isHost = nm.IsHost;
        bool isClient = nm.IsClient;
        sb.AppendLine($"Mode: {(isHost ? "Host" : "Client")} IsClient={isClient}");
        var po = nm.LocalClient?.PlayerObject;
        if (po == null)
        {
            sb.AppendLine("PlayerObject: N");
            _logText = sb.ToString();
            return;
        }
        sb.AppendLine("PlayerObject: Y");
        int childCount = po.transform.childCount;
        sb.AppendLine($"ChildCount: {childCount}");
        for (int i = 0; i < childCount && i < MaxChildNames; i++)
        {
            var c = po.transform.GetChild(i);
            sb.AppendLine($"  [{i}] {c.name}");
        }
        if (childCount > MaxChildNames)
            sb.AppendLine($"  ... +{childCount - MaxChildNames} more");
        var carriedVisual = po.GetComponentInChildren<CarriedItemVisual>(true);
        sb.AppendLine($"CarriedItemVisual: {(carriedVisual != null ? "Y" : "N")}");
        if (carriedVisual != null)
            sb.AppendLine($"  CarriedVisual.childCount: {carriedVisual.transform.childCount}");
        _logText = sb.ToString();
    }

    private void InitStyles()
    {
        if (_stylesInit) return;
        _boxStyle = new GUIStyle(GUI.skin.box);
        _boxStyle.normal.background = MakeTex(2, 2, backgroundColor);
        _labelStyle = new GUIStyle(GUI.skin.label);
        _labelStyle.alignment = TextAnchor.UpperLeft;
        _labelStyle.wordWrap = true;
        _stylesInit = true;
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var tex = new Texture2D(w, h);
        for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
                tex.SetPixel(i, j, col);
        tex.Apply();
        return tex;
    }
}
