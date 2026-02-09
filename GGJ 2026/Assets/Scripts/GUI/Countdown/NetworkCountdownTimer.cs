using Unity.Netcode;
using UnityEngine;
using System;

[RequireComponent(typeof(CircleCountdownUI))]
public class NetworkCountdownTimer : NetworkBehaviour
{
    [Header("倒计时设置")]
    [Tooltip("游戏总时长（秒）")]
    public float totalTime = 60f;

    [Tooltip("是否自动开始（网络生成后）")]
    public bool autoStart = false;

    [Header("危险时间阈值")]
    [Tooltip("最后几秒变红脉冲")]
    public float lowTimeThreshold = 10f;

    // 网络同步变量：剩余时间（Server写，Client读）
    private NetworkVariable<float> remainingTime = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> isRunning = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public event Action<float> OnTimeChanged;
    public event Action OnTimeUp;
    public event Action<float> OnProgressChanged;

    public float CurrentTime => remainingTime.Value;
    public float Progress => remainingTime.Value / totalTime;
    public bool IsRunning => isRunning.Value;
    public float LowTimeThreshold => lowTimeThreshold;

    // 🔥 修复核心：防止在同一帧内多次触发时间到逻辑
    private bool isProcessingTimeUp = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        remainingTime.OnValueChanged += OnRemainingTimeChanged;
        isRunning.OnValueChanged += OnIsRunningChanged;

        if (IsServer && autoStart)
        {
            StartTimerServerRpc();
        }
    }

    public override void OnNetworkDespawn()
    {
        remainingTime.OnValueChanged -= OnRemainingTimeChanged;
        isRunning.OnValueChanged -= OnIsRunningChanged;
        base.OnNetworkDespawn();
    }

    void Update()
    {
        // 只有服务器负责计算时间
        if (!IsServer) return;

        // 如果正在处理“时间到”的逻辑，由于可能会触发状态切换，必须暂停 Update
        if (isProcessingTimeUp) return;

        if (isRunning.Value)
        {
            float newTime = remainingTime.Value - Time.deltaTime;

            if (newTime <= 0)
            {
                // 1. 上锁！防止递归调用或重入
                isProcessingTimeUp = true;

                // 2. 归零并停止
                remainingTime.Value = 0;
                isRunning.Value = false;

                // 3. 触发事件
                // 注意：GameManager 会在这里响应，并可能立即调用 StartTimer 开始下一阶段（比如把时间设为 600）
                OnTimeUp?.Invoke();
                TimeUpClientRpc();

                // 4. 解锁
                isProcessingTimeUp = false;

                // 🔥🔥🔥 5. 必须 Return！绝对不能执行下面的赋值！🔥🔥🔥
                // 如果不 return，代码会继续往下走，把 remainingTime.Value 再次设为 newTime (<=0)
                // 这会覆盖掉 GameManager 刚才可能设置的新时间 (600)，导致下一帧立刻又判死刑
                return;
            }

            remainingTime.Value = newTime;
        }
    }

    public void SetTotalTime(float newTime)
    {
        if (!IsServer) return;
        totalTime = newTime;
    }

    private void OnRemainingTimeChanged(float oldValue, float newValue)
    {
        OnTimeChanged?.Invoke(newValue);
        // 防止除以0
        if (totalTime > 0)
            OnProgressChanged?.Invoke(newValue / totalTime);
    }

    private void OnIsRunningChanged(bool oldValue, bool newValue)
    {
        // 可以在这里加通用的开始/停止音效
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartTimerServerRpc()
    {
        if (totalTime > 0)
        {
            remainingTime.Value = totalTime;
            isRunning.Value = true;
            isProcessingTimeUp = false; // 确保锁是开的
            Debug.Log($"[Server] 游戏倒计时开始：{totalTime}秒");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopTimerServerRpc()
    {
        isRunning.Value = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddTimeServerRpc(float seconds)
    {
        remainingTime.Value = Mathf.Min(remainingTime.Value + seconds, totalTime);
    }

    [ClientRpc]
    private void TimeUpClientRpc()
    {
        // 🔥 修复Host双重触发：Host既是Server也是Client
        // Server端已经在 Update 里 Invoke 过了，这里必须拦截
        if (IsServer) return;

        OnTimeUp?.Invoke();
        // Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] 时间到！");
    }

    // 公共接口
    public void StartTimer() => StartTimerServerRpc();
    public void StopTimer() => StopTimerServerRpc();
    public void AddTime(float seconds) => AddTimeServerRpc(seconds);
}