# 联机大厅 (Lobby)

## 游戏时序（时间线）

| 阶段 | 时长 | 谁能动 | 说明 |
|------|------|--------|------|
| **1. 大厅 (Lobby)** | 直到 4 人齐 | 所有人可移动 | 四人在大厅内选面具（E 拾取），先到先得。 |
| **2. 追逐者冻结** | **5 秒**（LobbyManager `chaserFreezeSeconds`） | **仅求生者可移动** | 4 人齐后直接进入 Playing；边界扩大；追逐者原地冻结 5 秒，求生者可以跑。 |
| **3. 游戏进行 (Playing)** | **10 分钟**（GameManager `gameTime`） | 所有人可移动（追逐者冻结结束后） | 10 分钟倒计时；时间到则监管者胜利（可配置）。 |

**总结**：四人选好角色 → **直接开始** → **5 秒追逐者原地停留（求生者可动）** → **10 分钟正式对局**。  
时长可在 Inspector 中改：GameManager 的 `gameTime`，LobbyManager 的 `chaserFreezeSeconds`。（已取消 5 秒准备倒计时。）

---

## 功能概述

- 场景内玩家登入时**同一默认皮肤**；在**限制移动范围**的大厅内，四个台子放三张求生者面具 + 一张追逐者面具。
- 玩家按 **E** 范围交互拾取面具（先到先得），获得阵营与皮肤：求生者三种颜色、追逐者一种。
- **四人均拾取面具后**：解除移动范围限制、开始游戏；追逐者开局禁止移动若干秒；可选断开未拾取面具的玩家（只留四人）。

## 场景配置

1. **LobbyManager**
   - 空物体挂 `LobbyManager`，配置大厅边界 `lobbyBoundsMin/Max`、游戏边界 `gameBoundsMin/Max`、追逐者冻结秒数、是否断开未拾取玩家。
   - 若需“游戏开始”同步到客户端，同一物体加挂 `LobbyStateSync` + `NetworkObject`（并加入 NetworkManager 的 Prefab 列表或场景中生成）。

2. **GameManager**
   - 场景中需存在。大厅阶段 LobbyManager 会将其边界设为大厅范围；游戏开始后改为游戏范围（服务器直接设，客户端通过 LobbyStateSync 同步后设置）。

3. **四个面具台 (LobbyMaskPedestal)**
   - 每个台子一个物体，挂 `LobbyMaskPedestal`，配置 `maskIndex`：
     - 0、1、2：求生者三种颜色（同一阵营，不同皮肤）
     - 3：追逐者
   - 需有 Collider2D（用于 E 范围检测），可加 SpriteRenderer 显示面具图。

4. **玩家预制体**
   - 需有：`FactionMember`（初始阵营可留 -1）、`LayerInteractInput`、`PlayerMovement`、`PlayerImage`。
   - **PlayerImage**：配置 `defaultLobbySprite`（大厅默认皮肤）、`maskSprites`（长度 4：0/1/2 求生者，3 追逐者）。

5. **查看阵营**
   - 场景中任意物体挂 **FactionDisplayUI**（如挂在 Canvas 或 GameManager 上），运行后会在屏幕左上角显示当前阵营：「未选阵营」或「求生者」/「追逐者」。

6. **监管者开局冻结倒计时**
   - 与现有倒计时风格类似、但**放置在不同位置**：在 Canvas 下新建一个 UI 节点（如「ChaserFreezeCountdown」），挂 **ChaserFreezeCountdownUI**，并绑定：
     - **circleProgress**：一个 Image（Image Type 设为 Filled，Fill Method Radial 360）作为圆形进度；
     - **timeText**：TextMeshProUGUI 显示剩余秒数；
     - 可选 **flashEffect**：CanvasGroup 做低时间闪烁。
   - 将 **expectedFreezeDuration** 设为与 LobbyManager 的「追逐者开局禁止移动秒数」一致。仅当本地玩家为追逐者且处于冻结时显示。

## 与已有逻辑的关系

- **FactionMember**：大厅中初始为 -1（未选）；拾取面具后由服务器设为求生者(0)或追逐者(1)，并写入 MaskId(0~3)。
- **LayerInteractInput**：在大厅阶段且玩家尚未选面具时，优先检测范围内的 `LobbyMaskPedestal` 并拾取；否则走原有任务拾取/归还逻辑。
- **PlayerMovement**：追逐者开局冻结期间不移动（服务器与客户端均校验）；边界始终由 GameManager 限制（大厅/游戏由 LobbyManager 切换）。
- 非大厅场景若使用同一玩家预制体，可在 Inspector 将 FactionMember 的 `initialFactionId` 设为对应阵营。

## 有人连上时生成玩家（Spawn 位置）

- **玩家生成不由本工程自定义**：由 **Unity Netcode for GameObjects** 在客户端连接并被批准后自动完成。
- 在 **NetworkManager** 的 **Network Config** 里配置 **Player Prefab**；当新客户端连上并批准后，Netcode 会为该客户端 **Instantiate + SpawnAsPlayerObject(ownerClientId)** 该预制体。
- 本工程中 **NetworkManagerEx** 的 `OnClientConnected` 只做 `_serverCached = null`（清 Server 引用），**没有**在这里生成玩家；玩家生成逻辑在 Netcode 库的 `ConnectionManager.ApprovedPlayerSpawn` 中。
- 若「第二个客户端无法移动」，请检查：Player Prefab 是否正确配置、该预制体上是否有 `NetworkObject`、`PlayerMovement` 是否依赖 `IsOwner`/`GetLocalPlayerObject()` 等，以及服务器上该客户端的 PlayerObject 是否已正确 Spawn。

## 可选：只留四人

- LobbyManager 勾选 `disconnectPlayersWithoutMaskOnStart` 时，当四人拾取面具并开始游戏后，会断开所有未拾取面具的客户端，仅保留四名玩家。
