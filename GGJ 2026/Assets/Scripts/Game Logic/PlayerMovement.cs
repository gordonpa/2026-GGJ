using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

/// <summary>
/// 玩家移动脚本 - 修复权限报错版
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("不同角色可能有不同速度，如果为0则使用上面的默认值")]
    [SerializeField] private bool useVisualDataSpeed = true;

    [Header("障碍物检测")]
    [SerializeField] private LayerMask obstacleLayerMask = 0;
    [SerializeField] private float castSize = 0.4f;
    [SerializeField] private float wallEpsilon = 0.02f;

    [Header("视觉组件")]
    [SerializeField] private PlayerImage playerImage;
    [SerializeField] private SpriteRenderer playerSprite;

    [Header("动画")]
    [SerializeField] private string runParamName = "IsRunning";

    // 🔥 修复1：权限改为 Server，防止 RPC 修改失败
    private NetworkVariable<bool> netFacingRight = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 🔥 修复1：权限改为 Server
    private NetworkVariable<bool> netIsRunning = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private GameManager gameManager;
    private NetworkTransform networkTransform;
    private Rigidbody2D rb2d;
    private float currentSpeed;
    private bool wasMovingLastFrame = false;

    private Animator CurrentAnimator => playerImage?.GetAnimator();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        gameManager = FindObjectOfType<GameManager>();
        networkTransform = GetComponent<NetworkTransform>();
        rb2d = GetComponent<Rigidbody2D>();

        if (playerImage == null) playerImage = GetComponent<PlayerImage>();
        if (playerSprite == null)
        {
            playerSprite = GetComponent<SpriteRenderer>();
            if (playerSprite == null) playerSprite = GetComponentInChildren<SpriteRenderer>();
        }

        if (rb2d != null)
        {
            rb2d.isKinematic = true;
            rb2d.gravityScale = 0;
            rb2d.useFullKinematicContacts = true;
        }

        netFacingRight.OnValueChanged += OnDirectionChanged;
        netIsRunning.OnValueChanged += OnRunningStateChanged;

        if (playerSprite != null) playerSprite.flipX = !netFacingRight.Value;

        if (playerImage != null)
        {
            playerImage.OnVisualChanged += OnVisualChanged;
            UpdateSpeedFromVisualData();
        }
        currentSpeed = moveSpeed;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        netFacingRight.OnValueChanged -= OnDirectionChanged;
        netIsRunning.OnValueChanged -= OnRunningStateChanged;
        if (playerImage != null) playerImage.OnVisualChanged -= OnVisualChanged;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (IsChaserFrozen()) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = new Vector3(horizontal, vertical, 0f).normalized;
        bool isMoving = moveDirection.magnitude > 0.01f;

        // 处理动画和朝向
        HandleAnimationAndDirection(horizontal, isMoving);

        // 移动逻辑
        bool isServerAuthoritative = networkTransform != null && networkTransform.IsServerAuthoritative();

        if (isServerAuthoritative)
        {
            if (isMoving || wasMovingLastFrame)
            {
                MoveServerRpc(moveDirection, horizontal, isMoving);
            }
        }
        else
        {
            // Client Auth模式：本地计算
            Vector3 current = transform.position;
            Vector3 desired = current + moveDirection * currentSpeed * Time.deltaTime;
            Vector3 allowed = ClampPositionToObstacles(current, desired);

            if (gameManager != null) allowed = gameManager.ClampPositionToBounds(allowed);
            transform.position = allowed;
        }

        wasMovingLastFrame = isMoving;
    }

    void HandleAnimationAndDirection(float horizontal, bool isMoving)
    {
        // 1. 本地预表现（立刻更新Animator，不等服务器）
        var animator = CurrentAnimator;
        if (animator != null) animator.SetBool(runParamName, isMoving);

        // 2. 发送请求给服务器同步状态
        // 只有当状态真的改变时才发 RPC，节省流量
        if (netIsRunning.Value != isMoving)
        {
            SetRunningServerRpc(isMoving);
        }

        if (Mathf.Abs(horizontal) > 0.01f)
        {
            bool shouldFaceRight = horizontal > 0;
            if (shouldFaceRight != netFacingRight.Value)
            {
                SetDirectionServerRpc(shouldFaceRight);
            }
        }
    }

    void OnDirectionChanged(bool oldRight, bool newRight)
    {
        if (playerSprite == null) return;
        playerSprite.flipX = !newRight;
    }

    void OnRunningStateChanged(bool oldRunning, bool newRunning)
    {
        // 非Owner玩家依靠这个回调更新动画
        if (!IsOwner)
        {
            var animator = CurrentAnimator;
            if (animator != null) animator.SetBool(runParamName, newRunning);
        }
    }

    void OnVisualChanged(int visualId, PlayerImage.PlayerVisualData data)
    {
        UpdateSpeedFromVisualData();
    }

    void UpdateSpeedFromVisualData()
    {
        if (!useVisualDataSpeed || playerImage == null) return;
        var data = playerImage.GetCurrentData();
        currentSpeed = (data.moveSpeed > 0) ? data.moveSpeed : moveSpeed;
    }

    // 🔥 修复2：所有状态修改走 ServerRpc
    [ServerRpc(RequireOwnership = false)]
    void SetDirectionServerRpc(bool faceRight)
    {
        netFacingRight.Value = faceRight;
    }

    [ServerRpc(RequireOwnership = false)]
    void SetRunningServerRpc(bool isRunning)
    {
        netIsRunning.Value = isRunning;
    }

    [ServerRpc]
    private void MoveServerRpc(Vector3 moveDirection, float horizontalInput, bool isRunning)
    {
        var faction = GetComponent<FactionMember>();
        if (faction != null && faction.FactionId == LobbyConstants.FactionChaser && faction.ChaserFreezeUntil > 0)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < faction.ChaserFreezeUntil)
                return;
        }

        float speed = useVisualDataSpeed ? GetSpeedForVisualData() : moveSpeed;

        Vector3 current = transform.position;
        Vector3 desired = current + moveDirection * speed * Time.deltaTime;
        Vector3 allowed = ClampPositionToObstacles(current, desired);

        if (gameManager != null) allowed = gameManager.ClampPositionToBounds(allowed);
        transform.position = allowed;

        // 这里也可以顺便更新状态（双重保险）
        if (netIsRunning.Value != isRunning) netIsRunning.Value = isRunning;

        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            bool shouldFaceRight = horizontalInput > 0;
            if (netFacingRight.Value != shouldFaceRight) netFacingRight.Value = shouldFaceRight;
        }
    }

    float GetSpeedForVisualData()
    {
        if (playerImage == null) return moveSpeed;
        var data = playerImage.GetCurrentData();
        return data.moveSpeed > 0 ? data.moveSpeed : moveSpeed;
    }

    #region 状态检查

    private bool IsInReadyCountdown()
    {
        if (gameManager == null) return false;
        return gameManager.CurrentState == GameManager.GameState.ReadyCountdown;
    }

    private bool IsChaserFrozen()
    {
        var faction = GetComponent<FactionMember>();
        if (faction == null || faction.FactionId != LobbyConstants.FactionChaser) return false;
        if (faction.ChaserFreezeUntil <= 0) return false;
        var nm = NetworkManager.Singleton;
        if (nm == null) return false;
        return nm.ServerTime.Time < faction.ChaserFreezeUntil;
    }

    private Vector3 ClampPositionToObstacles(Vector3 current, Vector3 desired)
    {
        if (obstacleLayerMask == 0) return desired;
        float half = castSize * 0.5f;
        Vector2 size = new Vector2(castSize, castSize);
        Vector3 allowed = current;
        float deltaX = desired.x - current.x;
        if (Mathf.Abs(deltaX) > 0.0001f)
        {
            Vector2 origin = new Vector2(current.x, current.y);
            Vector2 dirX = deltaX > 0 ? Vector2.right : Vector2.left;
            RaycastHit2D hitX = Physics2D.BoxCast(origin, size, 0f, dirX, Mathf.Abs(deltaX) + half, obstacleLayerMask);
            if (hitX.collider != null) { float move = Mathf.Max(0f, hitX.distance - half - wallEpsilon); allowed.x = current.x + move * Mathf.Sign(deltaX); }
            else { allowed.x = desired.x; }
        }
        else { allowed.x = desired.x; }
        float deltaY = desired.y - current.y;
        if (Mathf.Abs(deltaY) > 0.0001f)
        {
            Vector2 originY = new Vector2(allowed.x, current.y);
            Vector2 dirY = deltaY > 0 ? Vector2.up : Vector2.down;
            RaycastHit2D hitY = Physics2D.BoxCast(originY, size, 0f, dirY, Mathf.Abs(deltaY) + half, obstacleLayerMask);
            if (hitY.collider != null) { float move = Mathf.Max(0f, hitY.distance - half - wallEpsilon); allowed.y = current.y + move * Mathf.Sign(deltaY); }
            else { allowed.y = desired.y; }
        }
        else { allowed.y = desired.y; }
        allowed.z = desired.z;
        return allowed;
    }

    void OnDrawGizmosSelected()
    {
        if (castSize <= 0) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(castSize, castSize, 0.1f));
    }
}
#endregion	
