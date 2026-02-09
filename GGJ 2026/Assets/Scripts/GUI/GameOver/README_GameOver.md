# 游戏结束结算 UI（GameOver）

## 功能

- 倒计时结束（或提前结束）时弹出结算界面，显示：**获胜方**、**赢家图标**、**排行榜**。
- 可选**调试面板**：不等待倒计时，输入输赢和三人排行榜后点击「显示结算」即可预览。

## 需要创建的物体与挂载

### 1. 结算面板（GameOverUI）

1. 在 **Canvas** 下新建空物体，命名为 **GameOverPanel**（或任意名）。
2. 在该物体上挂 **GameOverUI** 脚本。
3. 在 **GameOverPanel** 下建子物体并绑定：
   - **Panel Root**：拖自己（GameOverPanel）或该面板的根 GameObject，用于整体显示/隐藏。
   - **Winner Text**：一个 **Text**（或 TextMeshPro），用于显示「监管者胜利」/「求生者胜利」。
   - **Winner Icon**：一个 **Image**，用于显示赢家图标。
   - **Catcher Win Sprite** / **Runner Win Sprite**：在 Project 里选两张图作为监管者胜、求生者胜时的图标，拖到 GameOverUI 的这两个字段。
   - **Leaderboard Text**：一个 **Text**（或 TMP），用于多行显示排行榜（名次、名字、积分）。
4. 默认将 **Panel Root** 设为 **未激活**（或脚本 Start 时会关掉），游戏结束时再激活。

### 2. 排行榜数据源（可选）

- 若需要结算时显示排行榜，需实现 **IGameOverLeaderboardSource**（提供 `GetEntries(List<(string, int)>)`）。
- 在场景里建一个 **MonoBehaviour** 实现该接口（例如挂在 GameManager 同节点或空物体上），在 **GameManager** 的 **Game Over Leaderboard Source** 里拖入该物体。
- 不填则排行榜区域显示「（暂无排行榜）」。

### 3. 调试面板（可选）

1. 场景里**任意一个物体**（如 Canvas 或 GameManager 同节点）上挂 **GameOverDebugPanel**。
2. 可选：将 **Game Over UI** 拖到其 **Game Over UI** 字段；不填则运行时自动查找。
3. 运行后屏幕一角会出现调试区：选择「监管者胜」、输入 3 人姓名与积分，点击「显示结算」即可弹出结算界面，无需等倒计时。

## 配置小结

| 物体 | 挂载 | 配置 |
|------|------|------|
| Canvas 下 **GameOverPanel** | **GameOverUI** | Panel Root、Winner Text、Winner Icon、Catcher Win Sprite、Runner Win Sprite、Leaderboard Text |
| （可选）实现排行榜的物体 | 实现 **IGameOverLeaderboardSource** | 在 GameManager 的 **Game Over Leaderboard Source** 中拖入 |
| （可选）任意物体 | **GameOverDebugPanel** | 可选拖入 GameOverUI；位置/大小可在脚本里改 positionPercent、width |

## 接口说明

- **排行榜接口**：`IGameOverLeaderboardSource.GetEntries(List<(string displayName, int score)> outEntries)`，按名次从高到低填入即可；GameManager 结束游戏时会调用并同步给客户端。
- **调试**：`GameOverUI.ShowForDebug(bool catcherWin, IList<(string, int)> entries, Sprite winnerSprite)`，也可在代码里直接调用来测试。
