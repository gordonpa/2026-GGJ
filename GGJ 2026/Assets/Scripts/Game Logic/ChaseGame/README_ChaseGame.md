# 追逐/求生技能与图层 (ChaseGame)

## 背景

- 一个主图层 + 三个特殊图层；求生者面具 0/1/2 对应 Layer1/Layer2/Layer3。
- 追逐者：E 冲击波抓捕、J 切换图层（4 选 1）、I 大招（召唤求生者到当前图层并禁用求生者 J 一段时间）。
- 求生者：J 切换图层（主图层 + 面具图层，拾取死亡面具后可多选一个面具图层），拾取死亡求生者掉落的面具后 J 中可多一个“死亡面具所属图层”选项。

## 场景与预制体

1. **LayerMap**  
   - 场景需有 LayerMapManager，且游戏开始后为每个玩家生成 LayerMapClient（如调用 `LayerMapManager.Instance.ReqGenClient()`）。  
   - 图层显隐由 LayerMapSync + LayerSign 控制，掉落面具生成后会刷新 LayerSign 列表。

2. **玩家预制体（追逐者与求生者共用一个 prefab 时，以下组件全挂同一预制体即可）**  
   - 追逐者相关：`ChaserShockwaveAbility`（E，含冲击波圆形范围可视化）、`LayerMoveAbility`（J）、`ChaserUltimateAbility`（I）、`FactionMember`。  
   - 求生者相关：`SurvivorState`（死亡/携带死亡面具/技能禁用）、`LayerMoveAbility`（J）、`SurvivorDeathVisual`（死亡后隐藏外观）、`FactionMember`。  
   - 共用：`LayerInteractInput`（求生者按 E 可拾取掉落面具）、`PlayerMovement`（死亡求生者自动禁移）、**`PlayerLayerVisibility`**（仅同图层玩家可见且可碰撞；需在 Unity 中建 4 个 Layer 并设 2D 碰撞矩阵，见下方「玩家按图层显隐与碰撞」）。

3. **掉落面具（死亡求生者面具掉落、拾取、技能更新）**  
   - **谁在调用**：**不需要拖拽**。掉落物只在 **ChaserShockwaveAbility**（挂在玩家预制体上）里被生成：追逐者按 E 命中同图层求生者时，服务器用 `Resources.Load(droppedMaskPrefabName)` 按**字符串名**加载预制体并 Instantiate + Spawn。拾取由 **LayerInteractInput**（玩家按 E）触发。  
   - **注意**：**追逐者视角下会主动隐藏掉落物**（`DroppedMaskCollectible.Update` 对追逐者 SetActive(false)），所以用追逐者测试时“看不到掉落”是正常的，请用**求生者**视角或另一名求生者玩家确认主图层上是否出现掉落。  
   - **若面具没有掉落 / 没有 visual**，请按下面配置：
     1. **预制体**：新建预制体，命名为 `DroppedMaskCollectible`（或自定义后在 ChaserShockwaveAbility 的 `droppedMaskPrefabName` 里填同名）。  
     2. **组件**：根物体加 **NetworkObject**、**DroppedMaskCollectible**、**LayerSign**（Inspector 里 Layer 选 Main）、**Collider2D**（如 CircleCollider2D，勾选 Is Trigger）。  
     3. **显形**：在根物体或子物体上加 **SpriteRenderer** 并指定 Sprite（否则逻辑正常但没有可见图形）。  
     4. **位置**：将预制体放到 **Resources** 文件夹下（例如 `Assets/Resources/DroppedMaskCollectible.prefab`），`ChaserShockwaveAbility` 通过 `Resources.Load(droppedMaskPrefabName)` 加载。  
     5. **网络**：把该预制体加入 **NetworkManager** 的 **Network Prefabs List**，否则服务器 `Spawn()` 会失败或无法同步。  
   - 仅求生者可见、可拾取；追逐者不可见、不可拾取。拾取后求生者 J 技能中会多出「死亡面具所属图层」选项（见 LayerMoveUI）。
   - **测试「A 死、B 拾取 A、B 死、C 拾取 B」时**：掉落面具只能在主图层拾取；逻辑按 **clientId**（谁发 ServerRpc 谁就是拾取者）和 **OwnerClientId**（ClientRpc 只发给该玩家），与是否「主机」无关。  
     - 若 log 里拾取者总是 `clientId=0`：同一台电脑多开时，**键盘输入只会发给当前获得焦点的窗口**。要让 B（clientId=1）拾取，需先**点一下 B 所在窗口**使其获得焦点，再按 E；C 同理。  
     - J 选项 log 会打 `LocalClientId=X`，便于区分当前窗口是哪个 client（0/1/2/3）。

## 技能与 CD（可改）

- 追逐者 E：CD 5 秒（ChaserShockwaveAbility.cooldownSeconds）。  
- 追逐者 J：CD 5 秒（LayerMoveAbility.chaserCd）。  
- 追逐者 I：CD 3 分钟、求生者 J 禁用 1 分钟（ChaserUltimateAbility）。  
- 求生者 J：CD 20 秒（LayerMoveAbility.survivorCd）。

## UI（挂载与 NetworkObject）

- **SkillCDUI**、**LayerMoveUI**、**SurvivorSkillDisabledUI** 均可挂在场景中的**空物体**（或 Canvas 子物体）上，**三个都不需要挂 NetworkObject**：它们只读本地玩家状态并做展示，不参与网络生成与同步。
- **SkillCDUI**：技能图标与 CD 倒计时（E/J/I 按角色显示）。可配置可选贴图 `iconE`/`iconJ`/`iconI`（Texture2D），未设则显示 E/J/I 文字。  
- **LayerMoveUI**：J 打开图层选择面板，需绑定 LayerMoveAbility（或留空由脚本从本地玩家取）、**panelRoot**、4 个 Button（按顺序 Main/L1/L2/L3）。**四个按钮必须作为 panelRoot 的子物体**；脚本在 Start 中会对 panelRoot 执行 `SetActive(false)`，故进游戏时面板默认不显示，按 J 才弹出。  
- **SurvivorSkillDisabledUI**：追逐者大招后求生者“技能禁用”提示，可绑 panelRoot 或使用 OnGUI 文案。

## 倒计时 UI 区分（避免冲突）

- **ChaserFreezeCountdownUI**：只显示**追逐者开局 5 秒禁止移动**的剩余时间，数据来自 `FactionMember.ChaserFreezeUntil`，仅追逐者且处于冻结时显示。与通用倒计时无关。
- **CircleCountdownUI + NetworkCountdownTimer**：用于**整局倒计时**（5 秒准备 + 10 分钟游戏），由 **GameManager** 在 4 人齐后启动 5 秒准备，准备结束后再启动 10 分钟游戏。  
- **必须**将 **NetworkCountdownTimer.autoStart 设为 false**，否则进游戏后倒计时会立即开始（与“4 人齐后才 5 秒准备”冲突）。只有追逐者会被开局冻结 5 秒（LobbyManager.StartActualGame 只对 `FactionMember.IsChaser` 设置 ChaserFreezeUntil）；若出现“所有人被禁移”，请检查 NetworkCountdownTimer 是否误开了 autoStart，以及 GameManager 是否按 4 人齐 → 5 秒准备 → 准备结束 → StartActualGame 的顺序调用。

## 死亡与掉落

- 追逐者 E 范围内求生者会被“击杀”：SurvivorState.IsDead = true，其面具在主图层随机位置生成 DroppedMaskCollectible。  
- **死亡后玩家隐藏/禁移**：`SurvivorDeathVisual` 根据 IsDead 隐藏外观（可指定 `visualRoot` 或隐藏所有子物体 SpriteRenderer）；`PlayerMovement` 在求生者死亡时不再处理移动（客户端与 ServerRpc 均判断）。  
- 求生者拾取掉落面具后 SurvivorState.CarriedDeadMaskIndex 被设置，J 的图层选项中会多出该面具对应图层。

## 冲击波圆形范围可视化

- `ChaserShockwaveAbility` 中勾选 **Show Shockwave Range** 后，仅本地追逐者脚下会显示冲击波半径圆圈（LineRenderer），半径与 `shockwaveRadius` 一致，颜色与线段数可在 Inspector 中调整。

## 玩家按图层显隐（不改碰撞）

- **目标**：玩家只在「与本地玩家同一图层」时**显示**；追逐者 E 只能抓到同图层的求生者（逻辑过滤）。
- **物品**：场景中的物品/机关用 **LayerSign** 即可（LayerMapSync 会按本地玩家当前图层显隐），无需额外处理。
- **玩家**：在玩家预制体上挂 **PlayerLayerVisibility** 即可。该组件**只控制显隐**（同图层显示、不同图层隐藏），**不修改 gameObject.layer**，因此不会影响与地面/墙壁的碰撞，不会出现穿地、穿墙。
- **追逐者抓捕**：`ChaserShockwaveAbility` 只对「与追逐者当前图层相同」的求生者生效；不同图层的求生者不会被 E 抓到。
