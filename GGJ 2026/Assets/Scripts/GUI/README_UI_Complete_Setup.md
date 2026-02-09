# UI功能完整配置指南

本文档详细说明如何配置倒计时、排行榜、分数增加、胜利判定、名字输入限制、阵营显示等所有UI功能。

---

## 一、倒计时UI配置

### 1. 创建倒计时物体

1. 在场景中创建一个**空物体**，命名为 `CountdownTimer`
2. 添加以下组件：
   - **NetworkObject**（必须）
   - **NetworkCountdownTimer**（脚本）
   - **CircleCountdownUI**（脚本，会自动要求 NetworkCountdownTimer）

### 2. 配置 NetworkCountdownTimer

在 Inspector 中设置：
- **Total Time**: 游戏总时长（秒），例如 600（10分钟）
- **Auto Start**: 取消勾选（由 GameManager 控制）
- **Low Time Threshold**: 危险时间阈值（秒），例如 10（最后10秒变红）

### 3. 配置 CircleCountdownUI

在 Inspector 中绑定：
- **Circle Progress**: 拖入一个 **Image**（Fill 类型，用于显示进度圆环）
- **Time Text**: 拖入一个 **TextMeshProUGUI**（显示倒计时数字）
- **Flash Effect**: 拖入一个 **CanvasGroup**（可选，用于闪烁效果）
- **Game Manager**: 拖入场景中的 **GameManager**（用于区分准备阶段和游戏阶段）

### 4. 配置颜色渐变

在 **CircleCountdownUI** 的 **Color Gradient** 中设置：
- 左侧（0）：红色（危险时间）
- 右侧（1）：白色/绿色（正常时间）

### 5. 注册到 NetworkManager

- 将 `CountdownTimer` 物体**保存为预制体**
- 将预制体添加到 **NetworkManager** → **Network Config** → **Prefabs List**

### 6. 在 GameManager 中绑定

在 **GameManager** 的 Inspector 中：
- **Countdown Timer**: 拖入场景中的 `CountdownTimer` 物体

---

## 二、排行榜UI配置

### 1. 创建排行榜物体

1. 在场景中创建**任意一个物体**（空物体或 Canvas 下都可）
2. 添加 **ScoreboardUI** 组件

### 2. 配置 ScoreboardUI

在 Inspector 中设置：
- **UI Position Percent**: 位置百分比，例如 `(0.75, 0.01)`（右上角）
- **UI Size Percent**: 大小百分比，例如 `(0.24, 0.4)`
- **Custom Font**: 拖入支持中文的字体（可选）
- **Background Color**: 背景颜色，例如 `(0, 0, 0, 0.7)`
- **Text Color**: 文字颜色，例如白色
- **Local Player Color**: 本地玩家高亮颜色，例如黄色

### 3. UIMain 排行榜（当前 UICfg 主界面）需要挂载什么

**UIMain 里的排行榜（RankingText）数据来自 LayerMapClient.Score / PlayerName，不是 PlayerScore。** 要让分数正确增加，需要以下挂载与逻辑：

| 挂载/配置 | 说明 |
|----------|------|
| **场景中有 LayerMapManager** | 单例，用于管理所有玩家的 LayerMapClient（每个玩家一个，存 Score/PlayerName） |
| **场景中有 LayerMapClientBootstrap** | 挂到任意物体即可，游戏开始后会自动为每个玩家生成 LayerMapClient，否则 AllClient 为空、排行榜为空 |
| **Resources 中有 Server 预制体** | NetworkManagerEx 会 Spawn，上面有 Server（含 AddScoreServerRpc），提交时加分用 |
| **提交点挂 LayerSubmitZone** | 提交成功时会调用 `Server.AddScoreServerRpc(playerClientId, scorePerSubmit)`，在 Inspector 里可配置 **Score Per Submit**（默认 10） |
| **UIMain 预制体有 RankingText 节点** | TextMeshProUGUI，用于显示排行榜文本 |

**名字显示**：玩家名来自 LayerMapClient.PlayerName。若使用 UINameInput 输入名字，会调用 `Server.SetNameServerRpc` 写入，排行榜即显示该名字。

**无需**在 Player Prefab 上挂 PlayerScore 也能让 UIMain 排行榜正常加分、刷新；PlayerScore 是给 **ScoreboardUI** 用的（若你同时用了独立排行榜 UI）。

### 4. ScoreboardUI 排行榜（若使用独立排行榜 UI）

若场景里还有 **ScoreboardUI** 组件，它的数据来自 **PlayerScore**：

- **挂载位置**：**Player Prefab**（NetworkManager → Network Config → Player Prefab）
- **操作**：在玩家预制体根物体上添加 **PlayerScore** 组件；拾取/提交等加分逻辑需同时给 PlayerScore 加分（例如 Food 已调用 `playerScore.AddScoreServerRpc`；LayerSubmitZone 只给 LayerMapClient 加分，若也要更新 ScoreboardUI 需在提交逻辑里再给 PlayerScore 加分）。

### 5. 自动更新

UIMain 排行榜会在 `LayerMapManager.Instance.Client.Server.ScoreChangeRecordCount` 变化时刷新（提交加分会触发）；首次显示时也会刷新。

---

## 三、拾取/提交后分数增加配置

### 1. UIMain 排行榜分数（LayerMapClient）

**LayerSubmitZone 已内置**：提交成功时会调用 `Server.AddScoreServerRpc(playerClientId, scorePerSubmit)`，分数写入 LayerMapClient.Score，UIMain 排行榜会据此刷新。

- **配置方式**：在**提交点**物体上的 **LayerSubmitZone** 组件里，设置 **Score Per Submit**（默认 10），即单次提交加多少分。
- **前提**：场景有 **LayerMapManager** 和 **LayerMapClientBootstrap**，且 Server 预制体已 Spawn（见上文「UIMain 排行榜需要挂载什么」）。

### 2. 阵营总分（SurvivorScoreProvider，胜利判定用）

若胜利判定用阵营总分，在提交逻辑里**额外**调用：

```csharp
SurvivorScoreProvider.AddScore(10); // 根据设计调整
```

（LayerSubmitZone 已负责 UIMain 排行榜加分，若需要阵营总分可在此处或 LayerTaskManager 等处加。）

### 3. 分数重置

LayerMapClient.Score 由服务器管理；阵营总分会在游戏开始时由 `GameManager.StartGamePhase()` 等逻辑重置（若使用 SurvivorScoreProvider）。

---

## 四、胜利判定后结算画面配置

### 1. 创建结算面板

1. 在 **Canvas** 下创建空物体，命名为 `GameOverPanel`
2. 在 `GameOverPanel` 上添加 **GameOverUI** 组件

### 2. 创建子物体并绑定

在 `GameOverPanel` 下创建以下子物体：

#### Winner Text（获胜文本）
- 创建 **TextMeshProUGUI**，命名为 `WinnerText`
- 在 **GameOverUI** 的 Inspector 中，**Winner Text** 字段拖入此物体

#### Winner Icon（获胜图标）
- 创建 **Image**，命名为 `WinnerIcon`
- 在 **GameOverUI** 的 Inspector 中，**Winner Icon** 字段拖入此物体

#### Leaderboard Text（排行榜文本）
- 创建 **TextMeshProUGUI**，命名为 `LeaderboardText`
- 设置为**多行显示**（Vertical Overflow: Overflow）
- 在 **GameOverUI** 的 Inspector 中，**Leaderboard Text** 字段拖入此物体

### 3. 配置图标

准备两张图标：
- **Catcher Win Sprite**: 监管者胜利图标
- **Runner Win Sprite**: 求生者胜利图标

在 **GameOverUI** 的 Inspector 中分别拖入这两个字段。

### 4. 配置 Panel Root

在 **GameOverUI** 的 Inspector 中：
- **Panel Root**: 拖入 `GameOverPanel` 自己（或不填，脚本会用当前物体）

### 5. 配置 VictoryConditionManager

1. 在场景中**任意物体**上添加 **VictoryConditionManager** 组件（建议挂在 GameManager 同节点）
2. 在 Inspector 中设置：
   - **Win Score Threshold**: 获胜临界积分（例如 100）
   - **Game Manager**: 留空（会自动使用 `GameManager.Instance`）

### 6. 确保 GameManager 触发结算

`GameManager` 会在以下情况触发结算：
- 倒计时结束时（调用 `OnGameTimeout()`）
- 所有求生者被淘汰时（调用 `OnAllRunnersCaught()`）
- 求生者达到获胜分数时（由 `VictoryConditionManager` 调用 `OnRunnersWin()`）

确保 `GameManager` 有 `OnGameOver` 事件，`GameOverUI` 会自动订阅。

---

## 五、限制输入名字配置

### 方案A：使用 UINameInput（推荐，UIMgr 体系）

#### 1. 确保预制体存在
- 检查 `Resources/UI/UINameInput.prefab` 是否存在

#### 2. 确保场景有 UICfg
- 在场景中创建空物体，命名为 `UIRoot`
- 添加 **UICfg** 组件（场景中只能有一个）

#### 3. 调用名字输入
在需要的地方调用：
```csharp
UIMgr.Add<UINameInput>();
```

#### 4. 配置输入限制
在 `UINameInput.cs` 中，`TMP_InputField` 组件可以设置：
- **Character Limit**: 最大字符数（例如 20）
- **Content Type**: 文本类型（例如 Standard）

### 方案B：使用 PlayerNameInputUI（OnGUI 体系）

#### 1. 创建名字输入物体
- 在场景中创建空物体，命名为 `NameInputUI`
- 添加 **PlayerNameInputUI** 组件

#### 2. 配置位置和大小
在 Inspector 中设置：
- **UI Position Percent**: 例如 `(0.4, 0.4)`（屏幕中央）
- **UI Size Percent**: 例如 `(0.2, 0.15)`
- **Custom Font**: 拖入支持中文的字体（可选）

#### 3. 限制输入
在 `PlayerNameInputUI.cs` 第 111 行，`GUILayout.TextField` 的第二个参数是最大长度：
```csharp
playerNameInput = GUILayout.TextField(playerNameInput, 20, ...); // 20 是最大字符数
```

---

## 六、选择阵营后阵营名正确更新配置

### ⚠️ 重要：UIMain 预制体配置

**UIMain 预制体不能有 NetworkObject 组件！**

如果 `Resources/UI/UIMain.prefab` 有 NetworkObject 组件：
1. 打开 `Resources/UI/UIMain.prefab`
2. 检查是否有 **NetworkObject** 组件
3. 如果有，**删除它**（UI 不需要网络同步）
4. 保存预制体

代码已自动处理：如果检测到 NetworkObject，会自动移除并输出警告。

**UIMain 预制体需要包含以下子节点**（可选）：
- `CountdownText`（TextMeshProUGUI）- 倒计时显示（可选，如果没有则倒计时功能不显示）
- `CampText`（TextMeshProUGUI）- 阵营名称显示
- `CampImage`（Image）- 阵营图标（可选）
- `RankingText`（TextMeshProUGUI）- 排行榜显示
- `LeftTipText`（TextMeshProUGUI）- 左侧提示文本
- `MainSkill`、`SubSkill`、`LayerSkill`（SkillPrefab）- 技能显示
- `SkillList`（Transform）- 技能列表容器
- `SkillPrefab`（SkillPrefab）- 技能预制体模板

### 方案A：使用 FactionDisplayUI（OnGUI 体系）

#### 1. 创建阵营显示物体
- 在场景中创建空物体，命名为 `FactionDisplay`
- 添加 **FactionDisplayUI** 组件

#### 2. 配置显示位置
在 Inspector 中设置：
- **Position Percent**: 例如 `(0.02, 0.02)`（左上角）
- **Size Percent**: 例如 `(0.2, 0.06)`
- **Custom Font**: 拖入支持中文的字体（必须，否则中文显示为方框）

#### 3. 配置颜色
- **Survivor Color**: 求生者颜色（例如绿色）
- **Chaser Color**: 追逐者颜色（例如红色）

#### 4. 自动更新
`FactionDisplayUI` 会自动监听 `FactionMember` 的变化并更新显示，无需额外配置。

### 方案B：使用 UIMain（UIMgr 体系）

#### 1. 确保 UIMain 预制体存在
- 检查 `Resources/UI/UIMain.prefab` 是否存在

#### 2. 确保场景有 UICfg
- 场景中需要有 **UICfg** 组件（见"限制输入名字配置"）

#### 3. 显示 UIMain
在场景启动时调用：
```csharp
UIMgr.Change<UIMain>();
```

或在场景中创建空物体，添加 **ShowUIMainOnLoad** 组件（会自动在 Start 时显示 UIMain）。

#### 4. UIMain 中的阵营显示
`UIMain` 会自动从 `FactionMember` 读取阵营信息并显示在 **CampText** 中。

#### 5. 配置 UIMain 预制体
在 `Resources/UI/UIMain.prefab` 中：
- 确保有 **CampText**（TextMeshProUGUI）子物体
- 确保有 **CampImage**（Image）子物体（可选）

---

## 七、完整检查清单

### 倒计时UI
- [ ] 场景中有 `CountdownTimer` 物体，包含 NetworkObject、NetworkCountdownTimer、CircleCountdownUI
- [ ] CircleCountdownUI 绑定了 Circle Progress、Time Text、Game Manager
- [ ] CountdownTimer 预制体已注册到 NetworkManager Prefabs List
- [ ] GameManager 的 Countdown Timer 字段已绑定

### 排行榜UI
- [ ] 场景中有物体包含 ScoreboardUI 组件
- [ ] ScoreboardUI 的位置和大小已配置

### 分数增加
- [ ] LayerSubmitZone.SubmitServerRpc() 中已添加 `SurvivorScoreProvider.AddScore(分数)`
- [ ] 分数值已根据游戏设计调整

### 胜利判定和结算
- [ ] 场景中有 VictoryConditionManager 组件
- [ ] VictoryConditionManager 的 Win Score Threshold 已设置
- [ ] Canvas 下有 GameOverPanel，包含 GameOverUI 组件
- [ ] GameOverUI 绑定了 Winner Text、Winner Icon、Leaderboard Text
- [ ] 已准备 Catcher Win Sprite 和 Runner Win Sprite 图标

### 名字输入
- [ ] 场景中有 UICfg 组件（如果使用 UINameInput）
- [ ] Resources/UI/UINameInput.prefab 存在
- [ ] 或场景中有 PlayerNameInputUI 组件（如果使用 OnGUI 方案）

### 阵营显示
- [ ] 场景中有 FactionDisplayUI 组件（如果使用 OnGUI 方案）
- [ ] 或场景中有 UICfg 和 UIMain 相关配置（如果使用 UIMgr 方案）

---

## 八、常见问题

### Q: 倒计时不显示
- 检查 NetworkCountdownTimer 的 NetworkObject 是否已 Spawn
- 检查 CircleCountdownUI 的绑定是否正确
- 检查 GameManager 是否调用了 `countdownTimer.StartTimer()`

### Q: UIMain 排行榜为空 / 提交后分数没有正确增加
- **UIMain 排行榜数据来自 LayerMapClient**，不是 PlayerScore。请检查：
  1. **场景中有 LayerMapManager**（单例）
  2. **场景中有 LayerMapClientBootstrap**（挂到任意物体），否则不会为玩家生成 LayerMapClient，AllClient 为空、排行榜为空
  3. **提交点**上的 **LayerSubmitZone** 已配置 **Score Per Submit**（>0），提交成功时会调用 `Server.AddScoreServerRpc` 给该玩家的 LayerMapClient 加分
  4. **Resources 中有 Server 预制体**且已被 NetworkManagerEx Spawn，否则 AddScoreServerRpc 无人接收
- 若使用 **ScoreboardUI**（独立排行榜 UI），其数据来自 **PlayerScore**，需在 Player Prefab 上挂 PlayerScore，且加分逻辑里调用 `PlayerScore.AddScoreServerRpc`

### Q: 提交后分数不增加（阵营总分 / 胜利判定）
- 若胜利判定用阵营总分，检查提交逻辑是否调用了 `SurvivorScoreProvider.AddScore()`
- 检查 VictoryConditionManager 是否在检查分数

### Q: 结算画面不出现
- 检查 GameOverUI 是否订阅了 `GameManager.OnGameOver` 事件
- 检查 GameManager 是否在胜利时触发了 `OnGameOver` 事件
- 检查 GameOverPanel 是否默认隐藏（Start 时会设置为 false）

### Q: 名字输入限制不生效
- 检查 TMP_InputField 的 Character Limit 是否设置
- 检查 PlayerNameInputUI 的 TextField 第二个参数（最大长度）

### Q: 阵营名不更新
- 检查 FactionMember 组件是否存在
- 检查 FactionDisplayUI 或 UIMain 是否正确读取了 FactionMember
- 检查是否有支持中文的字体（否则中文显示为方框）

### Q: NetworkObject can only be reparented after being spawned
- **原因**：UIMain 预制体包含 NetworkObject 组件
- **解决**：打开 `Resources/UI/UIMain.prefab`，删除 NetworkObject 组件
- **注意**：代码已自动处理，但建议手动删除以避免警告

### Q: Node:CountdownText not find
- **原因**：UIMain 预制体中缺少 `CountdownText` 子节点
- **解决**：
  - 方案1：在 UIMain 预制体中添加名为 `CountdownText` 的 TextMeshProUGUI 子节点
  - 方案2：如果不需要倒计时显示，可以忽略此错误（代码已处理，不会崩溃）

---

## 九、代码修改点总结

### 必须挂载/配置（UIMain 排行榜）

1. **场景**：**LayerMapManager**、**LayerMapClientBootstrap**（任意物体上）
2. **提交点**：挂 **LayerSubmitZone**，Inspector 里设置 **Score Per Submit**（默认 10）
3. **Resources**：有 **Server** 预制体（NetworkManagerEx 会 Spawn）

无需再在 LayerSubmitZone 里手写 SurvivorScoreProvider.AddScore 来驱动 UIMain 排行榜；若需要阵营总分做胜利判定，可再在提交逻辑里加 SurvivorScoreProvider.AddScore。

### 可选修改的文件

1. **PlayerNameInputUI.cs**（第 111 行）
   - 修改 `GUILayout.TextField` 的第二个参数（最大字符数）

2. **UINameInput.cs**
   - 在预制体中配置 TMP_InputField 的 Character Limit

---

## 十、测试步骤

1. **测试倒计时**：启动游戏，检查倒计时是否正常显示和更新
2. **测试排行榜**：检查排行榜是否显示所有玩家及其分数
3. **测试分数增加**：拾取物品并提交，检查分数是否增加
4. **测试胜利判定**：达到获胜分数或倒计时结束，检查结算画面是否出现
5. **测试名字输入**：输入名字，检查是否有限制
6. **测试阵营显示**：选择阵营后，检查阵营名是否正确更新

