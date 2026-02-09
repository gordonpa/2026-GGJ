# 胜利判定（VictoryConditionManager）

## 规则摘要

- **阵营 ID**：求生者 = 0，追逐者 = 1（与 LobbyConstants 一致）。
- **求生者胜利（追逐者输）**  
  1. 倒计时结束前，求生者收集道具累加积分 **≥ 获胜临界值**；或  
  2. 倒计时结束时，**存活求生者人数 ≥ 1**。
- **追逐者胜利（求生者输）**  
  倒计时结束前，**存活求生者人数 = 0**（全部淘汰）。

## 场景配置

### 1. 挂载 VictoryConditionManager

- 在场景中**任意一个物体**上挂 **VictoryConditionManager**（建议挂在 **GameManager** 所在物体，或与 GameManager 同级的空物体上）。
- **Inspector 配置**：
  - **Win Score Threshold**：获胜临界积分（例如 100）。求生者总积分 ≥ 此值即判定求生者胜利。
  - **Game Manager**：留空则自动用 `GameManager.Instance`；若有多个 GameManager 可拖指定对象。

### 2. 积分接口（外部写入）

- 积分由 **SurvivorScoreProvider** 提供，**不在** VictoryConditionManager 里计算。
- 在「提交道具加分」等逻辑里调用：
  - `SurvivorScoreProvider.AddScore(单次积分);`
- 例如在 **LayerSubmitZone** 提交成功后、或在你自己的结算脚本里按道具/任务加对应分数。
- 每局开始时会自动把积分重置为 0（在 GameManager 进入 Playing 时）。

### 3. 倒计时与求生者数量

- **倒计时**：使用现有 **GameManager + NetworkCountdownTimer**，无需新建物体；时间到后由 GameManager 回调，VictoryConditionManager 已通过 `RunnerWinsOnTimeout` 注册「按存活人数」判定。
- **求生者数量**：由 **VictoryConditionManager.GetAliveSurvivorCount()** 在服务器上统计（FactionId=0 且 SurvivorState 未死亡），无需额外配置。

## 需要创建/挂载的物体

| 操作 | 说明 |
|------|------|
| **1 个物体挂 VictoryConditionManager** | 场景里任意物体（建议 GameManager 同节点或空物体），挂 **VictoryConditionManager**，配置 **Win Score Threshold**。 |
| **积分写入** | 在你已有的「提交道具 / 任务完成」等逻辑里调用 `SurvivorScoreProvider.AddScore(分数)`，无需新建物体。 |
| **GameManager** | 保持现有 GameManager 与倒计时绑定即可，无需新建。 |

## 小结

- 创建 **1 个**挂有 **VictoryConditionManager** 的物体，并设好 **Win Score Threshold**。
- 在提交道具（或其它加分点）处调用 **SurvivorScoreProvider.AddScore(...)**。
- 其余（倒计时、存活人数、胜负判定）已接好，无需再建物体或配置。
