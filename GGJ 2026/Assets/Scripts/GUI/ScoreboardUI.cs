using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 排行榜UI - 显示玩家积分和排行榜
/// </summary>
public class ScoreboardUI : MonoBehaviour
{
    [Header("UI设置")]
    [Tooltip("位置百分比 (0-1)，相对于屏幕")]
    [SerializeField] private Vector2 uiPositionPercent = new Vector2(0.75f, 0.01f);
    [Tooltip("大小百分比 (0-1)，相对于屏幕")]
    [SerializeField] private Vector2 uiSizePercent = new Vector2(0.24f, 0.4f);
    
    [Header("样式设置")]
    [SerializeField] private Font customFont;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color localPlayerColor = Color.yellow;
    
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private bool stylesInitialized = false;
    
    private void OnGUI()
    {
        InitializeStyles();
        
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsClient) return;
        
        DrawScoreboard();
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
        if (customFont != null)
        {
            labelStyle.font = customFont;
        }
        
        // 标题样式
        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.normal.textColor = textColor;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.fontSize = Mathf.RoundToInt(18 * (Screen.height / 1080f));
        if (customFont != null)
        {
            headerStyle.font = customFont;
        }
        
        stylesInitialized = true;
    }
    
    /// <summary>
    /// 绘制排行榜
    /// </summary>
    private void DrawScoreboard()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float posX = uiPositionPercent.x * screenWidth;
        float posY = uiPositionPercent.y * screenHeight;
        float width = uiSizePercent.x * screenWidth;
        float height = uiSizePercent.y * screenHeight;
        
        Rect scoreboardRect = new Rect(posX, posY, width, height);
        GUILayout.BeginArea(scoreboardRect);
        GUILayout.Box("", boxStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        
        GUILayout.BeginVertical();
        
        // 标题
        GUILayout.Label("排行榜", headerStyle);
        GUILayout.Space(10);
        
        // 获取所有玩家积分
        var players = GetPlayerScores();
        
        // 显示自己的积分
        var localPlayer = GetLocalPlayerScore();
        if (localPlayer.HasValue)
        {
            GUIStyle localStyle = new GUIStyle(labelStyle);
            localStyle.normal.textColor = localPlayerColor;
            GUILayout.Label($"我的积分: {localPlayer.Value.score}", localStyle);
            GUILayout.Space(5);
        }
        
        GUILayout.Space(5);
        
        // 显示排行榜
        int rank = 1;
        foreach (var player in players)
        {
            bool isLocalPlayer = localPlayer.HasValue && player.playerId == localPlayer.Value.playerId;
            GUIStyle style = isLocalPlayer ? new GUIStyle(labelStyle) { normal = { textColor = localPlayerColor } } : labelStyle;
            
            string displayName = player.playerName;
            if (isLocalPlayer)
            {
                displayName = $"我 ({displayName})";
            }
            
            GUILayout.Label($"{rank}. {displayName}: {player.score}", style);
            rank++;
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// 获取所有玩家积分
    /// </summary>
    private List<(ulong playerId, int score, string playerName)> GetPlayerScores()
    {
        var players = new List<(ulong playerId, int score, string playerName)>();
        
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null) return players;
        
        // 遍历所有已生成的网络对象
        foreach (var networkObject in networkManager.SpawnManager.SpawnedObjectsList)
        {
            if (networkObject.IsPlayerObject)
            {
                var playerScore = networkObject.GetComponent<PlayerScore>();
                if (playerScore != null)
                {
                    string name = playerScore.PlayerName;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = $"玩家 {playerScore.PlayerId}";
                    }
                    players.Add((playerScore.PlayerId, playerScore.Score, name));
                }
            }
        }
        
        // 按积分排序（降序）
        return players.OrderByDescending(p => p.score).ToList();
    }
    
    /// <summary>
    /// 获取本地玩家积分
    /// </summary>
    private (ulong playerId, int score, string playerName)? GetLocalPlayerScore()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || networkManager.LocalClient == null) return null;
        
        var localPlayerObject = networkManager.LocalClient.PlayerObject;
        if (localPlayerObject == null) return null;
        
        var playerScore = localPlayerObject.GetComponent<PlayerScore>();
        if (playerScore == null) return null;
        
        string name = playerScore.PlayerName;
        if (string.IsNullOrEmpty(name))
        {
            name = $"玩家 {playerScore.PlayerId}";
        }
        
        return (playerScore.PlayerId, playerScore.Score, name);
    }
    
    /// <summary>
    /// 更新排行榜（供外部调用）
    /// </summary>
    public void UpdateScoreboard()
    {
        // OnGUI会自动更新，这里可以添加额外的更新逻辑
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

