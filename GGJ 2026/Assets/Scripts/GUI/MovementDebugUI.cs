using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 在 GUI 上显示移动相关状态（便于单机 Editor 调试：为何不能移动）。
/// 必须挂到场景中任意物体上（如空物体或与 FactionDisplayUI 同一物体），否则左上角不会出现「移动调试」信息。
/// </summary>
public class MovementDebugUI : MonoBehaviour
{
    [Header("显示位置（屏幕比例 0-1，左上角为 (0,1)）")]
    [SerializeField] private Vector2 positionPercent = new Vector2(0.02f, 0.22f);
    [SerializeField] private Vector2 sizePercent = new Vector2(0.4f, 0.36f);

    [Header("样式")]
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.85f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color blockColor = new Color(1f, 0.4f, 0.3f);
    [SerializeField] private Color okColor = new Color(0.3f, 0.9f, 0.4f);

    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private bool stylesInitialized;
    private string debugText = "";
    private string blockReason = "";

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;
        RefreshMovementDebug();
    }

    private void RefreshMovementDebug()
    {
        var nm = NetworkManager.Singleton;
        var gm = GameManager.Instance ?? FindObjectOfType<GameManager>();
        var po = nm?.LocalClient?.PlayerObject;
        var faction = po != null ? po.GetComponent<FactionMember>() : null;
        var survivorState = po != null ? po.GetComponent<SurvivorState>() : null;

        bool isServer = nm != null && nm.IsServer;
        bool isClient = nm != null && nm.IsClient;
        ulong localId = nm != null && nm.LocalClient != null ? nm.LocalClient.ClientId : 999;
        bool hasPO = po != null;
        bool isOwner = hasPO && po.IsSpawned && po.IsOwner;
        ulong poOwnerId = hasPO ? po.OwnerClientId : 999;

        bool hasGM = gm != null;
        string stateStr = hasGM ? gm.CurrentState.ToString() : "-";
        bool isReadyCountdown = hasGM && gm.CurrentState == GameManager.GameState.ReadyCountdown;

        bool isChaserFrozen = false;
        if (faction != null && faction.FactionId == LobbyConstants.FactionChaser && faction.ChaserFreezeUntil > 0 && nm != null)
            isChaserFrozen = nm.ServerTime.Time < faction.ChaserFreezeUntil;

        bool isSurvivorDead = survivorState != null && survivorState.IsDead;

        int factionId = faction != null ? faction.FactionId : -999;
        double freezeUntil = faction != null ? faction.ChaserFreezeUntil : 0;
        double serverTime = nm != null ? nm.ServerTime.Time : 0;

        if (isReadyCountdown) blockReason = "Block: ReadyCountdown(5秒准备)";
        else if (isChaserFrozen) blockReason = "Block: ChaserFrozen(追逐者冻结)";
        else if (isSurvivorDead) blockReason = "Block: SurvivorDead(已死亡)";
        else if (!isOwner) blockReason = "Block: 非Owner(无法移动)";
        else blockReason = "Move: OK";

        debugText = $"连接: IsServer={isServer} IsClient={isClient} LocalId={localId}\n"
            + $"PlayerObject:{(hasPO ? "Y" : "N")} IsOwner={isOwner} PO.OwnerId={poOwnerId}\n"
            + $"GameManager:{(hasGM ? "Y" : "N")} CurrentState:{stateStr}\n"
            + $"Block: ReadyCD={isReadyCountdown} ChaserFrozen={isChaserFrozen} Dead={isSurvivorDead}\n"
            + $"FactionId={factionId} FreezeUntil={freezeUntil:F1} ServerTime={serverTime:F1}\n"
            + $"{blockReason}";
    }

    private void OnGUI()
    {
        InitStyles();
        float x = Screen.width * positionPercent.x;
        float y = Screen.height * positionPercent.y;
        float w = Screen.width * sizePercent.x;
        float h = Screen.height * sizePercent.y;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            GUI.Box(new Rect(x, y, w, 28), "", boxStyle);
            labelStyle.normal.textColor = textColor;
            GUI.Label(new Rect(x + 6, y + 4, w - 12, 20), "移动调试：未联网或非Client", labelStyle);
            return;
        }

        RefreshMovementDebug();
        GUI.Box(new Rect(x, y, w, h), "", boxStyle);
        labelStyle.normal.textColor = blockReason.StartsWith("Block") ? blockColor : okColor;
        GUI.Label(new Rect(x + 6, y + 6, w - 12, 24), blockReason, labelStyle);
        labelStyle.normal.textColor = textColor;
        labelStyle.fontSize = Mathf.RoundToInt(14 * (Screen.height / 1080f));
        GUI.Label(new Rect(x + 6, y + 30, w - 12, h - 36), debugText, labelStyle);
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, backgroundColor);
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.alignment = TextAnchor.UpperLeft;
        labelStyle.fontSize = Mathf.RoundToInt(16 * (Screen.height / 1080f));
        labelStyle.wordWrap = true;
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
