using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 胜利判定（仅服务器执行，不绑定 UI）：
/// 1. 求生者胜：阵营总分 >= winScoreThreshold。
/// 2. 监管者胜：本局有求生者且存活求生者 = 0（全部被抓）。
/// 3. 时间到：由 GameManager 倒计时结束触发，RunnerWinsOnTimeout 决定求生者/监管者胜。
/// Host 开启（服务器运行）后至少 minGraceSeconds 秒才会进行 1、2 的判定。
/// </summary>
public class VictoryConditionManager : MonoBehaviour
{
    [Header("胜利条件")]
    [Tooltip("求生者收集道具累加积分达到此值即求生者胜利")]
    public int winScoreThreshold = 100;

    [Header("判定时机")]
    [Tooltip("Host 开启（服务器运行）后至少多少秒才进行胜利判定")]
    [SerializeField] private float minGraceSeconds = 300f; // 5 分钟
    private double _serverStartTime = -1;

    [Header("监管者胜判定")]
    [Tooltip("进入 Playing 后多少秒内不判定「全部被抓」（避免开局即结束）")]
    [SerializeField] private float allCaughtGraceSeconds = 5f;
    private float _playingTime;

    [Header("引用（可空）")]
    [Tooltip("不填则用 GameManager.Instance")]
    public GameManager gameManager;

    [Header("调试")]
    [Tooltip("每 N 秒打印一次阵营总分与状态（0=不打印）")]
    [SerializeField] private float logScoreIntervalSeconds = 2f;
    private float _logTimer;
    private bool _loggedNotServer;
    private bool _loggedNoGameManager;

    private void Start()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (gameManager != null)
            gameManager.RunnerWinsOnTimeout = () => GetAliveSurvivorCount() >= 1;
    }

    private void OnDestroy()
    {
        if (gameManager != null && gameManager.RunnerWinsOnTimeout != null)
            gameManager.RunnerWinsOnTimeout = null;
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            if (!_loggedNotServer)
            {
                _loggedNotServer = true;
                Debug.LogWarning("[VictoryConditionManager] 仅服务器执行判定，当前不是 Server（Client 上不判定属正常）");
            }
            return;
        }
        if (gameManager == null)
        {
            if (!_loggedNoGameManager)
            {
                _loggedNoGameManager = true;
                Debug.LogWarning("[VictoryConditionManager] gameManager 为空，请确保场景有 GameManager");
            }
            return;
        }

        var nm = NetworkManager.Singleton;
        if (nm != null && _serverStartTime < 0)
            _serverStartTime = nm.ServerTime.Time;

        bool isPlaying = gameManager.CurrentState == GameManager.GameState.Playing;
        if (isPlaying)
            _playingTime += Time.deltaTime;
        else
            _playingTime = 0f;

        double serverTime = nm != null ? nm.ServerTime.Time : 0;
        bool graceElapsed = _serverStartTime >= 0 && (serverTime - _serverStartTime) >= (double)minGraceSeconds;

        if (!graceElapsed)
            return;

        int score = SurvivorScoreProvider.GetScore();
        if (logScoreIntervalSeconds > 0f && isPlaying)
        {
            _logTimer += Time.deltaTime;
            if (_logTimer >= logScoreIntervalSeconds)
            {
                _logTimer = 0f;
                Debug.Log($"[VictoryConditionManager] 阵营总分={score}, 胜利阈值={winScoreThreshold}, 已过宽限期");
            }
        }

        if (score >= winScoreThreshold)
        {
            Debug.Log($"[VictoryConditionManager] 阵营总分达标！{score} >= {winScoreThreshold}，触发求生者胜利");
            gameManager.OnRunnersWin();
            return;
        }

        int alive = GetAliveSurvivorCount();
        int totalSurvivors = GetTotalSurvivorCount();
        if (_playingTime >= allCaughtGraceSeconds && totalSurvivors > 0 && alive == 0)
            gameManager.OnAllRunnersCaught();
    }

    /// <summary>服务器：本局求生者总人数（FactionId=0，含已死亡）。</summary>
    public static int GetTotalSurvivorCount()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return 0;
        int count = 0;
        foreach (var kv in nm.ConnectedClients)
        {
            var po = kv.Value?.PlayerObject;
            if (po == null) continue;
            var faction = po.GetComponent<FactionMember>();
            if (faction == null || !faction.IsSurvivor) continue;
            count++;
        }
        return count;
    }

    /// <summary>服务器：当前存活的求生者人数（FactionId=0 且未死亡）。</summary>
    public static int GetAliveSurvivorCount()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return 0;

        int count = 0;
        foreach (var kv in nm.ConnectedClients)
        {
            var po = kv.Value?.PlayerObject;
            if (po == null) continue;

            var faction = po.GetComponent<FactionMember>();
            if (faction == null || !faction.IsSurvivor) continue;

            var surv = po.GetComponent<SurvivorState>();
            if (surv != null && surv.IsDead) continue;

            count++;
        }
        return count;
    }
}
