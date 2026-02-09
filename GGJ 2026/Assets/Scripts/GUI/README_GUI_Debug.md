# GUI 上的调试 / 日志信息

以下组件会在**屏幕 GUI**（非 Console）上显示调试或状态信息，便于单机/Editor 调试。

---

## 1. 移动调试（MovementDebugUI）

- **作用**：左上角显示「为何不能移动」：当前 Block 原因、GameManager 状态、FactionId、ChaserFreezeUntil、ServerTime 等。
- **位置**：默认在屏幕左上角偏下（约 2%, 22%），可调 `positionPercent` / `sizePercent`。
- **显示内容**：
  - 第一行：**Block: ReadyCountdown(5秒准备)** / **Block: ChaserFrozen(追逐者冻结)** / **Block: SurvivorDead(已死亡)** / **Move: OK**（红=被挡，绿=可动）
  - 下方：`GameManager:Y/N`、`CurrentState`、`IsInReadyCountdown`、`IsChaserFrozen`、`IsSurvivorDead`、`FactionId`、`ChaserFreezeUntil`、`ServerTime`
- **如何看到**：必须把 **MovementDebugUI** 挂到场景中**任意物体**上（如空物体或与 FactionDisplayUI 同一物体）。未挂则不会显示。
- **未联网时**：会显示一小条「移动调试：未联网或非Client」。

---

## 2. 阵营 + 图层调试（FactionDisplayUI）

- **作用**：左上角显示当前阵营（求生者/追逐者）和所属图层名；可选显示 **Layer 调试** 块。
- **位置**：默认左上角（约 2%, 2%）。
- **显示内容**：
  - 主框：`求生者 · 主图层` 或 `追逐者 · 图层1` 等（需指定支持中文的 `customFont` 才能正常显示中文）。
  - **Layer 调试**（Inspector 勾选 **Show Layer Debug** 时）：  
    `Layer调试 | 联网:Y/N LocalId:xxx`、`Instance:Y/N Client:Y/N AllClient数:x`、`Layer.Value:xxx layerName:"xxx"`。
- **如何看到**：把 **FactionDisplayUI** 挂到场景中任意物体上。  
- **关闭图层调试**：Inspector 里取消勾选 **Show Layer Debug**。

---

## 3. 其他 GUI（非“日志”）

- **SkillCDUI**：E/J/I 技能图标与 CD 显示。
- **SurvivorSkillDisabledUI**：求生者被大招禁用 J 技能时的提示。
- **ChaserFreezeCountdownUI**：追逐者开局冻结的圆形倒计时。
- **CircleCountdownUI**：5 秒准备 / 10 分钟游戏的倒计时圆环。
- **NetworkUIManager**：联机入口、IP 输入等。
- **FactionDisplayUI**：除调试块外，主框也是正常游戏 UI（阵营+图层名）。

---

## 4. 玩家子物体调试（PlayerChildrenDebugUI）

- **作用**：显示本地 **PlayerObject** 的 Mode(Host/Client)、子物体数量、前若干个子物体名字、是否有 **CarriedItemVisual** 及其子物体数。便于对比 Host 与 Client 下玩家子物体差异（如 item visual 是否只在 Client 出现）。
- **位置**：默认在屏幕左侧偏下（约 2%, 60%），可调 `positionPercent` / `sizePercent`。
- **如何看到**：把 **PlayerChildrenDebugUI** 挂到场景中任意物体上。

---

## 5. 通用 Log 面板（GameLogGUI）

- **作用**：在 GUI 上显示最近若干条 log，供 **ChaserShockwave**、掉落面具等调试用。任意脚本可调用 `GameLogGUI.AddLine(msg)` 或 `GameLogGUI.AddWarning(msg)` 写入，会同时打 Console 并显示在此面板。
- **位置**：默认屏幕左侧偏下（约 2%, 60% 起），可调 `positionPercent` / `sizePercent`。条数由 `maxLines` 控制（默认 12）。
- **如何看到**：把 **GameLogGUI** 挂到场景中任意物体上（可与 MovementDebugUI 同一物体）。未挂则只打 Console，不显示在 GUI。
- **当前写入**：ChaserShockwaveAbility 在「预制体未找到 / 无 NetworkObject」时写 Warning，「已生成掉落面具」时写 AddLine。

---

## 小结：想看 GUI 上的 log 要做什么

| 想看什么           | 挂载的组件           | 备注                         |
|--------------------|----------------------|------------------------------|
| 移动/状态调试      | **MovementDebugUI** | 必须挂上才有左上角移动调试   |
| 阵营 + 图层名      | **FactionDisplayUI**| 主框 + 可选 Layer 调试块     |
| 图层调试详情      | FactionDisplayUI     | 勾选 **Show Layer Debug**    |
| Host/Client 玩家子物体 | **PlayerChildrenDebugUI** | 对比 Host 与 Client 子物体差异 |
| 掉落面具等 log     | **GameLogGUI**      | ChaserShockwave 等写在此面板 |

以上均为 **OnGUI** 绘制，无需 Canvas；挂到任意 GameObject 即可。
