using Unity.Netcode;
using UnityEngine;
using LayerMap;

/// <summary>
/// 显示本地玩家当前阵营（拾取面具后：求生者/追逐者）及所属图层名称。挂到场景任意物体即可，无需 Canvas。
/// </summary>
public class FactionDisplayUI : MonoBehaviour
{
    [Header("显示位置与大小")]
    [Tooltip("屏幕相对位置 (0-1)，左上角为 (0,1)")]
    [SerializeField] private Vector2 positionPercent = new Vector2(0.02f, 0.02f);
    [SerializeField] private Vector2 sizePercent = new Vector2(0.2f, 0.06f);
    
    [Header("样式")]
    [Tooltip("中文（求生者/主图层等）需指定支持 CJK 的字体，否则会显示为方框或空白。在 Project 中导入 .ttf/.otf 后拖到此处。")]
    [SerializeField] private Font customFont;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.6f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color survivorColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color chaserColor = new Color(0.9f, 0.3f, 0.3f);

    [Header("调试（图层名不显示时可查看）")]
    [SerializeField] private bool showLayerDebug = true;

    private FactionMember localFaction;
    private string displayText = "";
    private string layerDebugText = "";
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private bool stylesInitialized;

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;
        if (localFaction == null)
        {
            var po = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (po != null)
                localFaction = po.GetComponent<FactionMember>();
        }
        if (localFaction == null) { displayText = ""; return; }
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (!localFaction.HasMask)
        {
            displayText = "未选阵营";
            return;
        }
        string faction = localFaction.IsSurvivor ? "求生者" : (localFaction.IsChaser ? "追逐者" : $"阵营 {localFaction.FactionId}");
        string layerName = GetCurrentLayerName();
        displayText = string.IsNullOrEmpty(layerName) ? faction : $"{faction} · {layerName}";
    }

    private string GetCurrentLayerName()
    {
        if (LayerMapManager.Instance == null) return "";
        var client = LayerMapManager.Instance.Client;
        if (client == null) return "";
        switch (client.Layer.Value)
        {
            case MapLayer.Main: return "主图层";
            case MapLayer.Layer1: return "图层1";
            case MapLayer.Layer2: return "图层2";
            case MapLayer.Layer3: return "图层3";
            default: return client.Layer.Value.ToString();
        }
    }

    private void RefreshLayerDebug()
    {
        var nm = NetworkManager.Singleton;
        bool hasNet = nm != null && nm.IsClient;
        bool hasInstance = LayerMapManager.Instance != null;
        int childCountRaw = hasInstance ? LayerMapManager.Instance.transform.childCount : 0;
        var client = hasInstance ? LayerMapManager.Instance.Client : null;
        bool hasClient = client != null;
        string layerVal = hasClient ? client.Layer.Value.ToString() : "-";
        string layerName = GetCurrentLayerName();
        ulong localId = (nm != null && nm.LocalClient != null) ? nm.LocalClient.ClientId : 999;
        int clientCount = hasInstance ? LayerMapManager.Instance.AllClient.Count : 0;
        bool reqGenRequested = LayerMapClientBootstrap.HasRequested;
        string notReqReason = LayerMapClientBootstrap.NotRequestedReason ?? "";
        if (!reqGenRequested && string.IsNullOrEmpty(notReqReason))
            notReqReason = "可能:Bootstrap未挂载或未启用";
        bool hasServer = Network.NetworkManagerEx.Instance != null && Network.NetworkManagerEx.Instance.Server != null;
        bool serverSpawned = hasServer && Network.NetworkManagerEx.Instance.Server.IsSpawned;
        layerDebugText = $"Layer调试 | 联网:{hasNet} LocalId:{localId}\n"
            + $"Instance:{(hasInstance ? "Y" : "N")} childCount(raw):{childCountRaw} AllClient数:{clientCount}\n"
            + $"Client:{(hasClient ? "Y" : "N")} Layer.Value:{layerVal} layerName:\"{layerName}\"\n"
            + $"Server:{(hasServer ? "Y" : "N")} Spawned:{(serverSpawned ? "Y" : "N")}\n"
            + $"ReqGenClient已请求:{(reqGenRequested ? "Y" : "N")} {(reqGenRequested ? "" : "原因:" + notReqReason)}";
    }

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;
        RefreshLayerDebug();
        InitStyles();
        float w = Screen.width * sizePercent.x;
        float h = Screen.height * sizePercent.y;
        float x = Screen.width * positionPercent.x;
        float y = Screen.height * positionPercent.y;
        GUI.Box(new Rect(x, y, w, h), "", boxStyle);
        if (localFaction != null && localFaction.HasMask)
            labelStyle.normal.textColor = localFaction.IsChaser ? chaserColor : survivorColor;
        else
            labelStyle.normal.textColor = textColor;
        GUI.Label(new Rect(x, y, w, h), displayText, labelStyle);

        if (showLayerDebug && !string.IsNullOrEmpty(layerDebugText))
        {
            float dw = Screen.width * 0.38f;
            float dh = Screen.height * 0.18f;
            float dy = y + h + 4f;
            GUI.Box(new Rect(x, dy, dw, dh), "", boxStyle);
            labelStyle.normal.textColor = textColor;
            int oldFontSize = labelStyle.fontSize;
            labelStyle.fontSize = Mathf.RoundToInt(14 * (Screen.height / 1080f));
            labelStyle.alignment = TextAnchor.UpperLeft;
            GUI.Label(new Rect(x + 4, dy + 4, dw - 8, dh - 8), layerDebugText, labelStyle);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = oldFontSize;
        }
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, backgroundColor);
        labelStyle = new GUIStyle(GUI.skin.label);
        if (customFont != null)
            labelStyle.font = customFont;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontSize = Mathf.RoundToInt(18 * (Screen.height / 1080f));
        stylesInitialized = true;
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
