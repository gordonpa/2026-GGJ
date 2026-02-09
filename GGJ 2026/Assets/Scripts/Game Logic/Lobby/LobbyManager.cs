using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 联机大厅：限制移动范围为大厅边界，四人拾取面具后解除限制并开始游戏，追逐者开局冻结若干秒。
/// 场景中需存在且仅存在一个。若需同步“游戏开始”到客户端，同一物体上挂 LobbyStateSync + NetworkObject。
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("边界")]
    [Tooltip("大厅阶段移动范围（四台子所在区域）")]
    [SerializeField] private Vector2 lobbyBoundsMin = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 lobbyBoundsMax = new Vector2(10f, 10f);
    [Tooltip("开始游戏后的地图范围")]
    [SerializeField] private Vector2 gameBoundsMin = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 gameBoundsMax = new Vector2(20f, 20f);

    public Vector2 GameBoundsMin => gameBoundsMin;
    public Vector2 GameBoundsMax => gameBoundsMax;

    [Header("开局")]
    [Tooltip("追逐者开局禁止移动秒数")]
    [SerializeField] private float chaserFreezeSeconds = 5f;
    [Tooltip("开始游戏时是否断开未拾取面具的玩家（只留四人）")]
    [SerializeField] private bool disconnectPlayersWithoutMaskOnStart = true;

    private bool gameStarted;
    private LobbyStateSync stateSync;

    public bool IsLobbyPhase => !gameStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        stateSync = GetComponent<LobbyStateSync>();
        ApplyLobbyBounds();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        ApplyLobbyBounds();
    }

    /// <summary>应用大厅边界到 GameManager（大厅阶段调用）。</summary>
    public void ApplyLobbyBounds()
    {
        if (GameManager.Instance != null && IsLobbyPhase)
            GameManager.Instance.SetBounds(lobbyBoundsMin, lobbyBoundsMax);
    }

    /// <summary>服务器：某玩家拾取面具后调用；若已有四人拾取则开始游戏。</summary>
    /// <summary>服务器：某玩家拾取面具后调用；若已有四人拾取则开始游戏。</summary>
    public void OnMaskPicked(ulong _)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (gameStarted) return;

        int countWithMask = 0;
        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var po = kv.Value.PlayerObject;
            if (po == null) continue;
            var f = po.GetComponent<FactionMember>();
            if (f != null && f.HasMask) countWithMask++;
        }

        if (countWithMask < 4) return;

        // 4人集齐：立即冻结抓捕者（准备阶段的5秒 = 冻结时间）
        double freezeEndTime = NetworkManager.Singleton.ServerTime.Time + (double)chaserFreezeSeconds;
        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var po = kv.Value.PlayerObject;
            if (po == null) continue;
            var f = po.GetComponent<FactionMember>();
            if (f != null && f.IsChaser)
                f.SetChaserFreezeUntilServer(freezeEndTime);
        }

        Debug.Log($"[LobbyManager] 4人已集齐，抓捕者冻结{chaserFreezeSeconds}秒，准备阶段开始");

        // 通知 GameManager 开始准备倒计时（5秒）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLobbyConditionsMet(this);
        }
        else
        {
            // 容错：如果没有 GameManager，直接开始游戏
            StartActualGame();
        }
    }
    /// <summary>
    /// 准备倒计时（5秒）结束后，由 GameManager 调用，执行实际游戏开始逻辑
    /// </summary>
    public void StartActualGame()
    {
        if (gameStarted) return;

        gameStarted = true;

        if (stateSync != null)
            stateSync.SetGameStartedServer(true);

        if (GameManager.Instance != null)
            GameManager.Instance.SetBounds(gameBoundsMin, gameBoundsMax);


        Debug.Log($"[LobbyManager] 游戏正式开始！追逐者冻结{chaserFreezeSeconds}秒");
    }
}
