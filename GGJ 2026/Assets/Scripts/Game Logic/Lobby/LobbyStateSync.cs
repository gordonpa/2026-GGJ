using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 大厅状态同步：将“游戏已开始”同步到所有客户端，客户端收到后应用游戏边界。需挂在带 NetworkObject 的物体上。
/// </summary>
public class LobbyStateSync : NetworkBehaviour
{
    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool GameStarted => gameStarted.Value;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        gameStarted.OnValueChanged += OnGameStartedChanged;
        if (gameStarted.Value)
            ApplyGameBounds();
    }

    public override void OnNetworkDespawn()
    {
        gameStarted.OnValueChanged -= OnGameStartedChanged;
        base.OnNetworkDespawn();
    }

    private void OnGameStartedChanged(bool _, bool newValue)
    {
        if (newValue)
            ApplyGameBounds();
    }

    private void ApplyGameBounds()
    {
        if (LobbyManager.Instance == null || GameManager.Instance == null) return;
        GameManager.Instance.SetBounds(LobbyManager.Instance.GameBoundsMin, LobbyManager.Instance.GameBoundsMax);
    }

    public void SetGameStartedServer(bool value)
    {
        if (IsServer)
            gameStarted.Value = value;
    }
}
