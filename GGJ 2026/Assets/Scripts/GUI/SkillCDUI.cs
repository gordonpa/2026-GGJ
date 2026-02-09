using UnityEngine;
using Unity.Netcode;
using LayerMap;

/// <summary>
/// 技能图标与 CD 倒计时：根据本地玩家角色显示 E/J/I 的 CD。可挂 Canvas 下或任意物体。
/// </summary>
public class SkillCDUI : MonoBehaviour
{
    [Header("位置与大小（屏幕比例 0-1）")]
    [SerializeField] private Vector2 positionPercent = new Vector2(0.02f, 0.88f);
    [SerializeField] private float slotWidth = 0.08f;
    [SerializeField] private float slotHeight = 0.08f;
    [SerializeField] private float spacing = 0.02f;

    [Header("样式")]
    [SerializeField] private Color bgColor = new Color(0, 0, 0, 0.5f);
    [SerializeField] private Color cdOverlayColor = new Color(0, 0, 0, 0.7f);
    [SerializeField] private Color textColor = Color.white;

    [Header("技能图标贴图（可选，未设则显示 E/J/I 文字）")]
    [Tooltip("追逐者 E 冲击波")]
    [SerializeField] private Texture2D iconE;
    [Tooltip("切换图层 J")]
    [SerializeField] private Texture2D iconJ;
    [Tooltip("追逐者大招 I")]
    [SerializeField] private Texture2D iconI;

    private FactionMember localFaction;
    private ChaserShockwaveAbility chaserShockwave;
    private LayerMoveAbility layerMove;
    private ChaserUltimateAbility chaserUltimate;
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private bool stylesInit;

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;
        var po = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (po == null) return;
        if (localFaction == null) localFaction = po.GetComponent<FactionMember>();
        if (chaserShockwave == null) chaserShockwave = po.GetComponent<ChaserShockwaveAbility>();
        if (layerMove == null) layerMove = po.GetComponent<LayerMoveAbility>();
        if (chaserUltimate == null) chaserUltimate = po.GetComponent<ChaserUltimateAbility>();
    }

    private void OnGUI()
    {
        if (localFaction == null) return;
        InitStyles();
        float w = Screen.width;
        float h = Screen.height;
        float x = w * positionPercent.x;
        float y = h * positionPercent.y;
        float sw = w * slotWidth;
        float sh = h * slotHeight;
        float sp = w * spacing;
        var nm = NetworkManager.Singleton;
        double now = nm != null ? nm.ServerTime.Time : 0;

        if (localFaction.IsChaser)
        {
            DrawSlot(x, y, sw, sh, "E", iconE, chaserShockwave != null ? chaserShockwave.NextUseTime : 0, now, chaserShockwave != null ? chaserShockwave.CooldownSeconds : 5);
            x += sw + sp;
            DrawSlot(x, y, sw, sh, "J", iconJ, layerMove != null ? layerMove.NextUseTime : 0, now, layerMove != null ? layerMove.ChaserCd : 5);
            x += sw + sp;
            DrawSlot(x, y, sw, sh, "I", iconI, chaserUltimate != null ? chaserUltimate.NextUseTime : 0, now, chaserUltimate != null ? chaserUltimate.CooldownSeconds : 180);
        }
        else if (localFaction.IsSurvivor)
        {
            DrawSlot(x, y, sw, sh, "J", iconJ, layerMove != null ? layerMove.NextUseTime : 0, now, layerMove != null ? layerMove.SurvivorCd : 20);
        }
    }

    private void DrawSlot(float x, float y, float sw, float sh, string key, Texture2D icon, double nextUse, double now, float cdDuration)
    {
        var rect = new Rect(x, y, sw, sh);
        GUI.Box(rect, "", boxStyle);
        float remaining = (float)(nextUse - now);
        if (remaining > 0 && cdDuration > 0)
        {
            float fill = Mathf.Clamp01(remaining / cdDuration);
            GUI.Box(new Rect(x, y + sh * (1f - fill), sw, sh * fill), "", boxStyle);
            string sec = Mathf.CeilToInt(remaining).ToString();
            labelStyle.normal.textColor = textColor;
            GUI.Label(rect, sec, labelStyle);
        }
        else
        {
            if (icon != null)
                GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
            else
            {
                labelStyle.normal.textColor = textColor;
                GUI.Label(rect, key, labelStyle);
            }
        }
    }

    private void InitStyles()
    {
        if (stylesInit) return;
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, bgColor);
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontSize = Mathf.RoundToInt(14 * (Screen.height / 1080f));
        stylesInit = true;
    }

    private static Texture2D MakeTex(int w, int h, Color c)
    {
        var t = new Texture2D(w, h);
        for (int i = 0; i < w; i++) for (int j = 0; j < h; j++) t.SetPixel(i, j, c);
        t.Apply();
        return t;
    }
}
