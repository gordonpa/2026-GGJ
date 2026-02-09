# 图层任务系统（寻物 → 携带 → 提交 → 完成标志）

## 流程

1. **图层**：多个背景图层可切换；当前图层由**外部**在切换时写入（见下方接口）。
2. **阵营**：每个玩家属于一个阵营（`FactionMember`）。
3. **寻物**：玩家切换到**特定图层**，在**该图层的特定物体**（`LayerCollectible`）上接触即可拾取。
4. **携带**：拾取后由 `CarriedItemHolder` 记录携带的物品 ID。
5. **提交**：玩家切换到**提交点所在图层**，进入**该图层的提交区域**（`LayerSubmitZone`）即提交。
6. **完成**：提交成功后，对外接口 `LayerTaskManager.IsFactionTaskCompleted(factionId)` 为 **true**，并触发 `OnFactionTaskCompleted`。

---

## 场景与预制体配置

### 1. LayerTaskManager（单例）

- 在场景中放一个空物体，挂 **LayerTaskManager**。
- 外部在**切换图层**时调用：`LayerTaskManager.SetCurrentLayer(图层ID)`。  
  **追逐/求生流程**：使用 J 切换图层时，**LayerMapClient** 已自动把当前 MapLayer 同步为 layerId（0=Main, 1=Layer1, 2=Layer2, 3=Layer3）并调用 `SetCurrentLayer`，无需改 LayerCollectible/LayerSubmitZone 的 layerId 逻辑。
- 当前图层也可在 Inspector 里设，用于测试。

### 2. 玩家预制体（Player Prefab）

- 在现有 Player Prefab 上添加：
  - **FactionMember**：阵营 ID 可在 Inspector 填 `Initial Faction Id`（0、1…），或运行时由服务器调用 `SetFactionServer(id)`。
  - **CarriedItemHolder**：无需配置，用于记录当前携带的任务物品。
  - **CarriedItemVisual**（可选）：拾取后若要在玩家身上**显示携带的物体**（如头顶/手中图标），在玩家根或任意子物体上加此组件并配置：
    - **Carried Visual Root**：拖玩家下的子物体，或 Project 里的预制体（将自动实例化并挂在 CarriedItemVisual 所在物体下）。显示时整棵子树会 SetActive(true)，隐藏时只对根 SetActive(false)。
    - **Sprites By Item Id**：按 ItemId 切换不同图标（索引 0、1… 对应 ItemId），可留空。脚本会从根或子物体上取 SpriteRenderer。
    - 拾取/提交时由 LayerCollectible / LayerSubmitZone 调用；此外 **CarriedItemVisual** 在每帧根据 **CarriedItemHolder.HasItem** 强制隐藏未携带时的显示，保证所有人不携带时都看不到 item 子物体。

### 3. 可拾取物（特定图层的“东西”）

- 新建 GameObject，挂 **LayerCollectible**，并配置：
  - **Layer Id**：该物品所在图层 ID。
  - **Item Id**：任务物品 ID（与提交点 Required Item Id 一致）。
  - **Faction Id Allowed**：可拾取的阵营（0 = 任意阵营）。
- 需有 **Collider2D（Is Trigger）** 和 **Rigidbody2D（Kinematic）**，脚本内会自动补全。
- 若为网络生成，需挂 **NetworkObject** 并加入 NetworkManager 的 Prefab 列表。

### 4. 提交点（特定图层的提交位置）

- 新建 GameObject（如空物体 + 碰撞盒），挂 **LayerSubmitZone**，并配置：
  - **Layer Id**：提交点所在图层 ID。
  - **Required Item Id**：需要提交的物品 ID（与 LayerCollectible 的 Item Id 一致）。
  - **Faction Id**：允许提交的阵营 ID。
- 需有 **Collider2D（Is Trigger）** 和 **Rigidbody2D（Kinematic）**，脚本内会自动补全。
- 若需网络同步该物体，再挂 **NetworkObject**；若仅作场景触发器可不挂。

---

## 对外接口（完成标志）

- **查询某阵营是否已完成任务**  
  `bool done = LayerTaskManager.IsFactionTaskCompleted(factionId);`  
  完成提交后为 **true**。

- **任务完成时收到通知**  
  `LayerTaskManager.OnFactionTaskCompleted += (factionId) => { ... };`

- **外部设置当前图层**（切换图层时调用）  
  `LayerTaskManager.SetCurrentLayer(layerId);`

---

## 示例配置

- 阵营 0 任务：在图层 1 拾取物品（ItemId=10），到图层 2 的提交点提交。
  - 可拾取物：LayerId=1, ItemId=10, FactionIdAllowed=0 或 1。
  - 提交点：LayerId=2, RequiredItemId=10, FactionId=0。
- 玩家切换到图层 1 → 接触可拾取物 → 自动拾取；切换到图层 2 → 进入提交点 → 提交成功，`IsFactionTaskCompleted(0)==true`。

---

## 拾取失败排查（Player 无法拾取 Item）

1. **Collider 必须为 Trigger**  
   可拾取物和玩家上的 2D 碰撞体都要勾选 **Is Trigger**，否则不会触发 `OnTriggerEnter2D`。脚本会在 Awake/OnValidate 里把可拾取物的 Collider 设为 Trigger。

2. **玩家预制体必须有**  
   - **CarriedItemHolder**  
   - **FactionMember**（并设置 Initial Faction Id，或运行时设阵营）  
   缺任意一个会在 Console 里看到对应警告。

3. **当前图层要一致**  
   - 场景里要有 **LayerTaskManager**（空物体挂脚本即可）。  
   - 可拾取物的 **Layer Id** 要和“当前图层”一致：  
     - 未设时当前图层为 **0**，可拾取物的 Layer Id 也要填 **0**；  
     - 或外部在切换图层时调用 `LayerTaskManager.SetCurrentLayer(layerId)`。

4. **Physics 2D 层碰撞**  
   Edit → Project Settings → Physics 2D → Layer Collision Matrix 中，玩家和可拾取物所在 Layer 要能互相碰撞，否则不会触发。

5. **2D 物理**  
   双方至少一方要有 **Rigidbody2D**（脚本会给可拾取物自动加 Kinematic Rigidbody2D）。Z 轴一致即可。
