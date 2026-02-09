using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 玩家名称输入UI - 允许玩家设置自己的名称
/// </summary>
public class PlayerNameInputUI : MonoBehaviour
{
    [Header("UI设置")]
    [Tooltip("位置百分比 (0-1)，相对于屏幕")]
    [SerializeField] private Vector2 uiPositionPercent = new Vector2(0.4f, 0.4f);
    [Tooltip("大小百分比 (0-1)，相对于屏幕")]
    [SerializeField] private Vector2 uiSizePercent = new Vector2(0.2f, 0.15f);
    
    [Header("样式设置")]
    [SerializeField] private Font customFont;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
    [SerializeField] private Color textColor = Color.white;
    
    private string playerNameInput = "";
    private bool showNameInput = true;
    private bool nameSet = false;
    
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle textFieldStyle;
    private GUIStyle buttonStyle;
    private bool stylesInitialized = false;
    
    private void OnGUI()
    {
        if (!showNameInput || nameSet) return;
        
        InitializeStyles();
        
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsClient) return;
        
        // 检查是否有本地玩家
        if (networkManager.LocalClient == null || networkManager.LocalClient.PlayerObject == null) return;
        
        DrawNameInputUI();
    }
    
    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        // Box样式
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, backgroundColor);
        if (customFont != null)
        {
            boxStyle.font = customFont;
        }
        
        // 标签样式
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = textColor;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        if (customFont != null)
        {
            labelStyle.font = customFont;
        }
        
        // 输入框样式
        textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.normal.textColor = textColor;
        if (customFont != null)
        {
            textFieldStyle.font = customFont;
        }
        
        // 按钮样式
        buttonStyle = new GUIStyle(GUI.skin.button);
        if (customFont != null)
        {
            buttonStyle.font = customFont;
        }
        
        stylesInitialized = true;
    }
    
    /// <summary>
    /// 绘制名称输入UI
    /// </summary>
    private void DrawNameInputUI()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float posX = uiPositionPercent.x * screenWidth;
        float posY = uiPositionPercent.y * screenHeight;
        float width = uiSizePercent.x * screenWidth;
        float height = uiSizePercent.y * screenHeight;
        
        Rect inputRect = new Rect(posX, posY, width, height);
        GUILayout.BeginArea(inputRect);
        GUILayout.Box("", boxStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        
        GUILayout.BeginVertical();
        GUILayout.Space(10);
        
        // 标题
        GUILayout.Label("输入玩家名称", labelStyle);
        GUILayout.Space(10);
        
        // 输入框
        playerNameInput = GUILayout.TextField(playerNameInput, 20, textFieldStyle, GUILayout.Height(30));
        
        GUILayout.Space(10);
        
        // 确认按钮
        if (GUILayout.Button("确认", buttonStyle, GUILayout.Height(30)))
        {
            SetPlayerName();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// 设置玩家名称
    /// </summary>
    private void SetPlayerName()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || networkManager.LocalClient == null) return;
        
        var playerObject = networkManager.LocalClient.PlayerObject;
        if (playerObject == null) return;
        
        var playerScore = playerObject.GetComponent<PlayerScore>();
        if (playerScore == null) return;
        
        // 如果输入为空，使用默认名称
        if (string.IsNullOrEmpty(playerNameInput.Trim()))
        {
            playerScore.SetPlayerName($"玩家 {playerScore.PlayerId}");
        }
        else
        {
            playerScore.SetPlayerName(playerNameInput.Trim());
        }
        
        nameSet = true;
        showNameInput = false;
    }
    
    /// <summary>
    /// 创建纯色纹理
    /// </summary>
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}

