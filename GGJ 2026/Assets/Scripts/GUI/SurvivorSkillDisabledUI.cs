using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 求生者普通技能（J）被禁用时的可视化：追逐者大招后显示“技能禁用”提示。挂到场景任意 UI 下。
/// </summary>
public class SurvivorSkillDisabledUI : MonoBehaviour
{
    [Header("显示")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("若未绑定 panelRoot，则用 OnGUI 在屏幕中央显示文字")]
    [SerializeField] private bool fallbackToGui = true;
    [SerializeField] private string disabledText = "技能禁用中";
    [SerializeField] private Color textColor = new Color(1f, 0.3f, 0.3f);

    private FactionMember localFaction;
    private SurvivorState localSurvivorState;
    private GUIStyle labelStyle;
    private bool styleInit;

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;
        var po = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (po == null) return;
        if (localFaction == null) localFaction = po.GetComponent<FactionMember>();
        if (localSurvivorState == null) localSurvivorState = po.GetComponent<SurvivorState>();

        bool show = localFaction != null && localFaction.IsSurvivor && localSurvivorState != null && localSurvivorState.IsSkillDisabled();
        if (panelRoot != null)
            panelRoot.SetActive(show);
    }

    private void OnGUI()
    {
        if (!fallbackToGui) return;
        if (localFaction == null || !localFaction.IsSurvivor || localSurvivorState == null || !localSurvivorState.IsSkillDisabled()) return;
        if (!styleInit)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = Mathf.RoundToInt(24 * (Screen.height / 1080f));
            labelStyle.normal.textColor = textColor;
            styleInit = true;
        }
        labelStyle.normal.textColor = textColor;
        float w = Screen.width * 0.4f;
        float h = 40f;
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height * 0.15f;
        GUI.Label(new Rect(x, y, w, h), disabledText, labelStyle);
    }
}
