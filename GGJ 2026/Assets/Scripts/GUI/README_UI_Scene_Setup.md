# UI 放到场景里的具体流程

项目里有两类 UI 用法，放到场景的方式不同。

---

## 一、UIMain 体系（UIMgr + UICfg）：**代码调用 → 从 Resources 加载 Prefab 生成**

### 是不是「空物体上写脚本调一下就生成 UI」？

**是的。** 流程是：

1. **场景里先准备好「UI 根」**
   - 在 Hierarchy 里建一个**空物体**（例如命名为 `UI Root`）。
   - 挂上 **UICfg** 脚本（且场景里只能有一个，单例）。
   - 运行后 UICfg 的 `Awake` 会把自己的 `gameObject` 设为 `Root`，之后所有通过 UIMgr 打开的 UI 都会生成在这个物体下面。

2. **Resources 里要有对应 Prefab**
   - 路径必须是：`Resources/UI/<类名>.prefab`。
   - 例如 UIMain → `Resources/UI/UIMain.prefab`（项目里已有）。

3. **在「当前场景的任意物体」上写脚本，需要时调用 UIMgr**
   - 例如在空物体上挂 **TestUIMain**，或你自己写的脚本。
   - 在合适时机（如按钮、流程节点）调用：
     - `UIMgr.Change<UIMain>()`：切换到主界面（会从 Resources 加载 UIMain.prefab 并显示）。
     - `UIMgr.Add<UINameInput>()`：叠加显示取名界面。
     - 其他 `UIMgr.Get<T>()` / `UIMgr.Show<T>()` 等同理。
   - **第一次**调用 `UIMgr.Get<T>()` 或 `Change<T>()` 时：
     - UIMgr 发现还没有该 UI 实例；
     - 调用 `UICfg.LoadUI<T>()` → `Resources.Load<GameObject>("UI/UIMain")` 加载 Prefab；
     - `Instantiate` 后挂到 **UICfg.Root** 下，并执行 `Init()`、默认先 `Hide`，再按你调用的 `Change`/`Show` 显示。

### 小结（UIMain 类 UI）

| 步骤 | 做什么 |
|------|--------|
| 1 | 场景里建空物体，挂 **UICfg**（UI 根，只需一个） |
| 2 | 确保 `Resources/UI/UIMain.prefab` 等 Prefab 存在 |
| 3 | 在任意物体上挂脚本，在需要的地方写 `UIMgr.Change<UIMain>()` 等 |
| 4 | 运行后，第一次调用时就会自动从 Resources 加载并生成 UI，**不需要**在 Hierarchy 里事先拖 UIMain |

**希望一进场景就显示 UIMain**：在挂 **UICfg** 的同一个物体上再挂 **ShowUIMainOnLoad**，运行后会在 `Start` 时自动调 `UIMgr.Change<UIMain>()`，主界面会一直显示。

---

## 二、排行榜 / 结算类 UI：**场景里直接摆物体 + 挂脚本 + 绑引用**

这类 UI **不经过 UIMgr**，不会从 Resources 自动生成，而是**在场景里手动建好**。

### 1. 游戏中实时排行榜（ScoreboardUI）

- **脚本**：`ScoreboardUI.cs`，用 `OnGUI` 画排行榜。
- **放到场景**：
  1. 在 Hierarchy 里**任意一个物体**（空物体或 Canvas 下都可）上挂 **ScoreboardUI**。
  2. 运行即有排行榜；位置/大小在 Inspector 里调 `Ui Position Percent`、`Ui Size Percent` 等。
- **不需要**拖 Prefab、不需要 UIMgr，挂上就生效。

### 2. 游戏结束结算（GameOverUI + 可选 GameOverDebugPanel）

- **脚本**：`GameOverUI.cs`（结算面板）、可选 `GameOverDebugPanel.cs`（调试用）。
- **放到场景**（按 [README_GameOver.md](GameOver/README_GameOver.md)）：
  1. 在 **Canvas** 下建空物体，命名如 **GameOverPanel**。
  2. 在 **GameOverPanel** 上挂 **GameOverUI**。
  3. 在 GameOverPanel 下建子物体并**在 Inspector 里绑定**：
     - **Panel Root**：拖 GameOverPanel 自己（或不填，脚本会用当前物体）。
     - **Winner Text**：TextMeshPro 文本，显示「监管者胜利」/「求生者胜利」。
     - **Winner Icon**：Image，赢家图标。
     - **Leaderboard Text**：TextMeshPro，多行排行榜。
     - **Catcher Win Sprite** / **Runner Win Sprite**：两张图标。
  4. （可选）在**任意物体**上挂 **GameOverDebugPanel**，运行后屏幕一角可点「显示结算」调试。

这类 UI 都是：**在场景里建好 GameObject 和子物体 → 挂脚本 → 绑好引用**，不会通过「调一句 UIMgr」自动生成。

---

## 对比一览

| 类型           | 是否用 UIMgr | 如何放到场景 |
|----------------|-------------|--------------|
| UIMain 等面板  | 是          | 场景里建空物体挂 **UICfg**；任意脚本里调 `UIMgr.Change<UIMain>()` 等，从 **Resources/UI/** 加载 Prefab 生成 |
| ScoreboardUI   | 否          | 任意物体挂 **ScoreboardUI** 即可 |
| GameOverUI     | 否          | Canvas 下建 Panel，挂 **GameOverUI**，下面建文本/图片并绑好引用 |

所以：**UIMain 那种**是在当前场景空物体（或任意物体）上写脚本、调一下 `UIMgr.Change<UIMain>()` 就能生成 UI，前提是场景里有 **UICfg** 且 **Resources/UI** 下有对应 Prefab；**排行榜/结算**则是把现有脚本挂到场景里已有的物体上并绑定好引用即可。
