using System.Linq;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 游戏管理器 - 管理游戏边界和基本规则
/// </summary>
public partial class GameManager : NetworkBehaviour
{
    [Header("游戏边界设置")]
    [SerializeField] private Vector2 boundsMin = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 boundsMax = new Vector2(20f, 20f);
    
    [Header("边界可视化（Scene 视图用 Gizmos，Game 视图用下方运行时线框）")]
    [SerializeField] private bool showBounds = true;
    [SerializeField] private Color boundsColor = Color.red;
    [Tooltip("在 Game 视图中显示边界线框")]
    [SerializeField] private bool showBoundsInGame = true;
    [Tooltip("边界线框线宽")]
    [SerializeField] private float boundsLineWidth = 0.05f;

    [Header("调试")]
    [Tooltip("运行时自动开始游戏（跳过4人限制，仅测试用）")]
    public bool autoStartGameOnAwake = false;

    private static GameManager instance;
    private LineRenderer boundsLine;
    
    public static GameManager Instance => instance;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (showBoundsInGame)
            BuildBoundsVisual();

        Debug.Log("[GameManager] 网络初始化检查...");

        InitializeCountdown();  // ← 新增：初始化倒计时系统（不启动，只绑定事件）

        StartCoroutine(InitializeWithDelay());
    }

    System.Collections.IEnumerator InitializeWithDelay()
    {
        // 等待 NetworkManager 出现并启动（最多等10秒）
        float timeout = 10f;
        float elapsed = 0f;

        Debug.Log("[GameManager] 等待 NetworkManager 启动...");

        while ((NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) && elapsed < timeout)
        {
            yield return null; // 每帧检查
            elapsed += Time.deltaTime;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[GameManager] 等待超时，NetworkManager 未启动！");
            yield break;
        }

        Debug.Log($"[GameManager] NetworkManager 已就绪（等待了{elapsed:F1}秒），IsServer={NetworkManager.Singleton.IsServer}");

        if (countdownTimer == null)
        {
            yield break;
        }

        // 绑定事件
        countdownTimer.OnTimeUp += () => Debug.Log("[GameManager] 时间到！");

    }


    void DebugForceStart()
    {
        StartReadyCountdown(); // 直接调准备阶段，跳过所有条件
    }

    /// <summary>
    /// 获取边界内的随机位置
    /// </summary>
    public Vector3 GetRandomPositionInBounds()
    {
        float x = Random.Range(boundsMin.x, boundsMax.x);
        float y = Random.Range(boundsMin.y, boundsMax.y);
        return new Vector3(x, y, 0f);
    }
    
    /// <summary>
    /// 检查位置是否在边界内
    /// </summary>
    public bool IsPositionInBounds(Vector3 position)
    {
        return position.x >= boundsMin.x && position.x <= boundsMax.x &&
               position.y >= boundsMin.y && position.y <= boundsMax.y;
    }

    private new void OnDestroy()
    {
        CleanupCountdown();
    }

    /// <summary>
    /// 将位置限制在边界内
    /// </summary>
    public Vector3 ClampPositionToBounds(Vector3 position)
    {
        float x = Mathf.Clamp(position.x, boundsMin.x, boundsMax.x);
        float y = Mathf.Clamp(position.y, boundsMin.y, boundsMax.y);
        return new Vector3(x, y, position.z);
    }
    
    /// <summary>
    /// 设置边界
    /// </summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        boundsMin = min;
        boundsMax = max;
        RefreshBoundsVisual();
    }
    
    private void BuildBoundsVisual()
    {
        if (boundsLine != null) return;
        var go = new GameObject("BoundsVisual");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        boundsLine = go.AddComponent<LineRenderer>();
        boundsLine.useWorldSpace = true;
        boundsLine.loop = true;
        boundsLine.positionCount = 5;
        boundsLine.startWidth = boundsLineWidth;
        boundsLine.endWidth = boundsLineWidth;
        boundsLine.material = new Material(Shader.Find("Sprites/Default"));
        boundsLine.startColor = boundsColor;
        boundsLine.endColor = boundsColor;
        boundsLine.sortingOrder = 32767;
        boundsLine.sortingLayerName = "Default";
        RefreshBoundsVisual();
    }
    
    private void RefreshBoundsVisual()
    {
        if (!showBoundsInGame || boundsLine == null) return;
        float z = 0f;
        boundsLine.SetPosition(0, new Vector3(boundsMin.x, boundsMin.y, z));
        boundsLine.SetPosition(1, new Vector3(boundsMax.x, boundsMin.y, z));
        boundsLine.SetPosition(2, new Vector3(boundsMax.x, boundsMax.y, z));
        boundsLine.SetPosition(3, new Vector3(boundsMin.x, boundsMax.y, z));
        boundsLine.SetPosition(4, new Vector3(boundsMin.x, boundsMin.y, z));
    }
    
    private void OnValidate()
    {
        if (boundsLine != null && Application.isPlaying)
            RefreshBoundsVisual();
    }
    
    /// <summary>
    /// 获取边界
    /// </summary>
    public void GetBounds(out Vector2 min, out Vector2 max)
    {
        min = boundsMin;
        max = boundsMax;
    }
    
    private void OnDrawGizmos()
    {
        if (!showBounds) return;
        
        // 在Scene视图中绘制边界
        Gizmos.color = boundsColor;
        Vector3 center = new Vector3((boundsMin.x + boundsMax.x) / 2f, (boundsMin.y + boundsMax.y) / 2f, 0f);
        Vector3 size = new Vector3(boundsMax.x - boundsMin.x, boundsMax.y - boundsMin.y, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}


/// <summary>
/// GameManager 多阶段倒计时与胜负逻辑
/// </summary>
public partial class GameManager : NetworkBehaviour
{
    [Header("倒计时系统")]
    [Tooltip("场景中的倒计时器（需放在NetworkObject上）")]
    public NetworkCountdownTimer countdownTimer;

    [Header("准备阶段设置")]
    [Tooltip("准备倒计时（秒），4人齐后开始")]
    public float readyTime = 5f;

    [Header("游戏阶段设置")]
    [Tooltip("游戏总时长（10分钟 = 600秒）")]
    public float gameTime = 600f;

    [Header("胜负规则")]
    [Tooltip("时间耗尽后监管者胜利（若 VictoryConditionManager 注册了 RunnerWinsOnTimeout 则按存活人数判定，忽略此项）")]
    public bool catcherWinsOnTimeout = true;

    /// <summary>由 VictoryConditionManager 设置：时间耗尽时求生者是否胜利（如存活≥1 则 true）。为 null 时用 catcherWinsOnTimeout。</summary>
    public System.Func<bool> RunnerWinsOnTimeout { get; set; }

    [Header("结算 UI 数据源（可选）")]
    [Tooltip("不填则排行榜为空")]
    public MonoBehaviour gameOverLeaderboardSource;
    private IGameOverLeaderboardSource _leaderboardSource;

    // 网络同步游戏状态
    public enum GameState
    {
        Lobby,          // 大厅等待
        ReadyCountdown, // 准备倒计时（5秒）
        Playing,        // 游戏进行中（10分钟）
        CatcherWin,     // 监管者胜利
        RunnerWin       // 逃跑者胜利
    }

    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(
        GameState.Lobby,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 当前倒计时类型（用于UI区分）
    public enum CountdownType { None, Ready, Game }
    public CountdownType CurrentCountdownType { get; private set; } = CountdownType.None;

    public GameState CurrentState => gameState.Value;
    public bool IsInGame => gameState.Value == GameState.Playing;

    // 缓存LobbyManager引用，准备结束后回调用
    private LobbyManager pendingLobbyManager;

    // 初始化（在原GameManager的Start中调用）
    private void InitializeCountdown()
    {
        if (countdownTimer == null)
        {
            Debug.LogError("[GameManager] 未绑定CountdownTimer！");
            return;
        }

        // 订阅倒计时事件
        countdownTimer.OnTimeUp += OnCountdownFinished;
        countdownTimer.OnTimeChanged += OnCountdownChanged;

        gameState.OnValueChanged += OnGameStateChanged;

        if (IsServer)
        {
            gameState.Value = GameState.Lobby;
            CurrentCountdownType = CountdownType.None;
        }
    }

    // 清理（在原GameManager的OnDestroy中调用）
    private void CleanupCountdown()
    {
        if (countdownTimer != null)
        {
            countdownTimer.OnTimeUp -= OnCountdownFinished;
            countdownTimer.OnTimeChanged -= OnCountdownChanged;
        }
        gameState.OnValueChanged -= OnGameStateChanged;
    }

    /// <summary>
    /// LobbyManager 调用：4人齐，开始5秒准备倒计时
    /// </summary>
    public void OnLobbyConditionsMet(LobbyManager lobbyManager)
    {
        Debug.Log($"[GameManager] OnLobbyConditionsMet被调用 - IsServer={IsServer}, CurrentState={gameState.Value}");

        if (!IsServer)
        {
            Debug.LogWarning("[GameManager] 不是Server，忽略");
            return;
        }

        if (gameState.Value != GameState.Lobby)
        {
            Debug.LogWarning($"[GameManager] 状态不对，当前是{gameState.Value}，期望是Lobby");
            return;
        }

        if (!IsServer) return;
        if (gameState.Value != GameState.Lobby) return;

        pendingLobbyManager = lobbyManager;
        StartReadyCountdown();  // 恢复5秒准备倒计时
    }

    private void StartReadyCountdown()
    {
        Debug.Log($"[GameManager] StartReadyCountdown被调用 - countdownTimer={(countdownTimer != null)}, readyTime={readyTime}");

        if (gameState.Value != GameState.Lobby)
        {
            Debug.LogWarning("[GameManager] 状态不是Lobby，无法开始准备倒计时");
            return;
        }

        gameState.Value = GameState.ReadyCountdown;
        CurrentCountdownType = CountdownType.Ready;

        if (countdownTimer == null)
        {
            Debug.LogError("[GameManager] countdownTimer为null！请在Inspector中绑定！");
            return;
        }

        // 关键：确保设置时长并启动
        countdownTimer.SetTotalTime(readyTime);
        Debug.Log($"[GameManager] 设置倒计时时间为{readyTime}秒，开始调用StartTimer()");

        countdownTimer.StartTimer();
        OnReadyPhaseStartedClientRpc();
    }

    /// <summary>
    /// 阶段2：准备结束，开始10分钟游戏（白色/红色）
    /// </summary>
    private void StartGamePhase()
    {
        SurvivorScoreProvider.SetScore(0);
        Debug.Log("[Server] 准备结束！游戏正式开始，10分钟倒计时...");
        gameState.Value = GameState.Playing;
        CurrentCountdownType = CountdownType.Game;

        // 关键：通知 LobbyManager 执行扩大边界、冻结监管者等实际操作
        if (pendingLobbyManager != null)
        {
            pendingLobbyManager.StartActualGame();
            pendingLobbyManager = null;
        }

        // 开始10分钟游戏倒计时
        countdownTimer.SetTotalTime(gameTime);
        countdownTimer.StartTimer();

        // 通知客户端游戏阶段开始
        OnGamePhaseStartedClientRpc();
    }

    /// <summary>
    /// 倒计时进行中（每秒更新，UI订阅此事件）
    /// </summary>
    private void OnCountdownChanged(float remaining)
    {
        // 可以在这里做服务器端的每秒逻辑（如同步给特定客户端）
    }

    /// <summary>
    /// 倒计时结束回调：区分准备阶段(5秒)还是游戏阶段(10分钟)
    /// </summary>
    private void OnCountdownFinished()
    {
        // 🔥🔥🔥 新增这行：如果是客户端，直接无视，不许改状态！
        if (!IsServer) return;

        switch (gameState.Value)
        {
            case GameState.ReadyCountdown:
                // 5秒准备结束，进入游戏
                StartGamePhase();
                break;

            case GameState.Playing:
                // 10分钟耗尽，监管者胜利
                OnGameTimeout();
                break;
        }
    }

    /// <summary>
    /// 游戏时间耗尽：若已注册 RunnerWinsOnTimeout 则按存活人数判定（≥1 求生者胜），否则用 catcherWinsOnTimeout。
    /// </summary>
    private void OnGameTimeout()
    {
        if (!IsServer) return;

        bool runnerWins = RunnerWinsOnTimeout != null ? RunnerWinsOnTimeout() : !catcherWinsOnTimeout;
        Debug.Log($"[Server] 游戏时间耗尽！{(runnerWins ? "求生者" : "监管者")}胜利");
        EndGame(runnerWins ? GameState.RunnerWin : GameState.CatcherWin);
    }

    /// <summary>
    /// 结束游戏
    /// </summary>
    private void EndGame(GameState finalState)
    {
        if (gameState.Value == finalState) return;

        gameState.Value = finalState;
        CurrentCountdownType = CountdownType.None;

        string winner = finalState == GameState.CatcherWin ? "监管者" : "逃跑者";
        Debug.Log($"[Server] 游戏结束！{winner}胜利");

        if (_leaderboardSource == null && gameOverLeaderboardSource != null)
            _leaderboardSource = gameOverLeaderboardSource as IGameOverLeaderboardSource;
        if (_leaderboardSource != null)
        {
            var list = new List<(string displayName, int score)>();
            _leaderboardSource.GetEntries(list);
            string encoded = GameOverLeaderboardProvider.EncodeEntries(list);
            SetGameOverLeaderboardClientRpc(encoded);
        }
        else
            SetGameOverLeaderboardClientRpc("");

        ShowGameOverClientRpc(finalState == GameState.CatcherWin);
        // 不再禁用玩家移动：胜利后仍可移动（已移除 DisableAllPlayersClientRpc 调用）
    }

    // ========== RPC 通知客户端 ==========

    [ClientRpc]
    private void OnReadyPhaseStartedClientRpc()
    {
        Debug.Log("[Client] 准备阶段开始（5秒）");
        // UI可以在这里做本地特效（如镜头拉近、屏幕变黄提示）
    }

    [ClientRpc]
    private void OnGamePhaseStartedClientRpc()
    {
        Debug.Log("[Client] 游戏正式开始（10分钟）");
        // 可以在这里播放"游戏开始"音效
    }

    [ClientRpc]
    private void SetGameOverLeaderboardClientRpc(string encoded)
    {
        GameOverLeaderboardProvider.SetEntriesFromEncoded(encoded);
    }

    /// <summary>游戏结束时触发，参数为 true=监管者胜，false=求生者胜。结算 UI 可订阅。</summary>
    public static System.Action<bool> OnGameOver;

    [ClientRpc]
    private void ShowGameOverClientRpc(bool catcherWin)
    {
        string msg = catcherWin ? "监管者胜利！" : "逃跑者胜利！";
        Debug.Log($"[Client] 游戏结束：{msg}");
        OnGameOver?.Invoke(catcherWin);
        // 若 GameOverPanel 初始为未激活，GameOverUI.Awake 可能未执行、未订阅，用查找兜底确保结算面板一定会显示
        EnsureGameOverPanelShown(catcherWin);
        StartCoroutine(InvokeGameOverAgainNextFrame(catcherWin));
    }

    /// <summary>查找 GameOverUI（含未激活）并调用 Show，确保结算面板一定会亮起。</summary>
    private void EnsureGameOverPanelShown(bool catcherWin)
    {
        var ui = FindObjectOfType<GameOverUI>(true);
        if (ui != null)
        {
            ui.Show(catcherWin, null);
            Debug.Log("[GameManager] EnsureGameOverPanelShown: 已对 GameOverUI 调用 Show");
        }
        else
            Debug.LogWarning("[GameManager] EnsureGameOverPanelShown: 场景中未找到 GameOverUI");
    }

    /// <summary>延迟一帧再触发一次，避免首次触发时 GameOverUI 尚未就绪导致结算界面不显示。</summary>
    private System.Collections.IEnumerator InvokeGameOverAgainNextFrame(bool catcherWin)
    {
        yield return null;
        OnGameOver?.Invoke(catcherWin);
    }

    [ClientRpc]
    private void DisableAllPlayersClientRpc()
    {
        var localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (localPlayer != null)
        {
            var movement = localPlayer.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;
        }
    }

    // ========== 公共方法 ==========

    /// <summary>
    /// 服务器强制开始（Debug用，跳过准备阶段）
    /// </summary>
    public void ForceStartGame()
    {
        if (!IsServer) return;
        pendingLobbyManager = null; // 不需要回调LobbyManager
        StartGamePhase();
    }

    /// <summary>
    /// 监管者抓完所有求生者时调用（提前结束）
    /// </summary>
    public void OnAllRunnersCaught()
    {
        if (!IsServer || gameState.Value == GameState.CatcherWin) return;
        EndGame(GameState.CatcherWin);
    }

    /// <summary>
    /// 求生者达成目标时调用（如收集完所有物品）
    /// </summary>
    public void OnRunnersWin()
    {
        if (!IsServer || gameState.Value == GameState.RunnerWin) return;
        EndGame(GameState.RunnerWin);
    }

    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        // 状态变化时的通用处理（如播放音效）
    }
}