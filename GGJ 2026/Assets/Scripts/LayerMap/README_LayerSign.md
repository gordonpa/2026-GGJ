# LayerSign 图层标记说明

挂有 **LayerSign** 的物体，会根据**本地玩家当前所在图层**自动显隐：玩家在该图层时显示，不在时隐藏（除非勾选 Follow）。

---

## Inspector 里要设置的项

| 属性 | 说明 |
|------|------|
| **Layer** | 该物体属于哪个图层：**Main**（主图层）、**Layer1**、**Layer2**、**Layer3**。玩家当前图层与这里一致时，物体才会显示（Follow 为 false 时）。 |
| **Follow** | 是否“跟随玩家图层”。勾选后该物体**始终显示**，不随玩家切换图层而隐藏。适合 UI、全图层可见的机关等。不勾选则按 Layer 做显隐。 |
| **Owned Objs** | 被控制显隐的节点列表。**不填**时会在 `Awake` 里自动填入**所有子物体**；也可手动拖入要一起显隐的 Transform。 |

---

## 使用方式

1. **按图层显隐的物体**  
   - 给物体（或父节点）挂 **LayerSign**。  
   - 将 **Layer** 设为对应图层（如主图层用 Main，某副图层用 Layer1/2/3）。  
   - **Follow** 不勾选。  
   - 该物体及其子物体（或 Owned Objs 里的节点）会在“玩家当前图层 = Layer”时显示，否则隐藏。

2. **全图层都显示的物体**  
   - 挂 **LayerSign**，勾选 **Follow** 即可，Layer 可任意。

3. **只控制子节点**  
   - 父物体挂 LayerSign，子物体会在切换图层时自动显隐（Awake 会填满子节点到 Owned Objs）。若只想控制其中几个，可清空 Owned Objs 后手动拖入需要的 Transform。

---

## 依赖

- 场景里要有 **LayerMapManager**，且本地玩家已生成 **LayerMapClient**（通过 **LayerMapClientBootstrap** 自动请求）。  
- 每个 **LayerMapClient** 上带有 **LayerMapSync**，会 `FindObjectsOfType<LayerSign>` 并按其 **Layer** 与玩家当前图层比较后调用 **ShowOrHide**。  
- 若 **LayerMapManager.Instance.Client** 为 null（未生成 Client），`RefreshLayerStatus` 会报错，需先保证 Client 生成（挂 LayerMapClientBootstrap）。

---

## 多图层布景（只看到/交互当前图层）

场景里要有多层“布景”，玩家只能看到和交互**当前所在图层**的物体时，用现有脚本按下面方式挂载和配置即可。

### 场景前提（必须已有）

- 场景中有 **LayerMapManager**（单例）。
- 玩家预制体通过 **LayerMapClientBootstrap** 自动请求生成 **LayerMapClient**；每个 LayerMapClient 上带 **LayerMapSync**，会按本地玩家当前图层刷新所有 **LayerSign** 的显隐。
- 场景中有 **LayerTaskManager**（空物体挂脚本）；切换图层时 **LayerMapClient** 会把当前 MapLayer 同步为 layerId（0=Main, 1=Layer1, 2=Layer2, 3=Layer3）并调用 `SetCurrentLayer`，无需额外配置。

### 1. 纯布景（只显隐、不交互）

| 挂载 | 配置 |
|------|------|
| **LayerSign** | **Layer**：该布景所在图层（Main / Layer1 / Layer2 / Layer3）。**Follow** 不勾选。**Owned Objs** 不填则 Awake 自动填入所有子物体；也可手动拖入要一起显隐的 Transform。 |

- 玩家在该图层时显示，不在时隐藏。**LayerSign 只控制子节点显隐**，不控制自身根物体；若希望“整块布景”一起显隐，请把 **LayerSign 挂在父物体上**，布景内容放在子物体里（或把子物体拖进 Owned Objs）。

### 2. 可拾取物（特定图层拾取、按 E 交互）

| 挂载 | 配置 |
|------|------|
| **父物体** | 只挂 **LayerSign**：**Layer** = 该物品所在图层（Main/Layer1/Layer2/Layer3），**Follow** 不勾选，**Owned Objs** 默认子物体即可。 |
| **子物体** | **LayerCollectible**：**Layer Id** = 0/1/2/3（0=Main, 1=Layer1, 2=Layer2, 3=Layer3，与 LayerSign 的 Layer 对应）。**Item Id**、**Faction Id Allowed** 按任务需求填。**Collider2D（Is Trigger）**、**Rigidbody2D（Kinematic）** 需有（脚本可自动补全）。若网络生成则再加 **NetworkObject**。 |

- 显隐由父物体 **LayerSign** 控制；交互（拾取）由 **LayerCollectible** + **LayerTaskManager.CurrentLayerId** 判定，玩家只有在该图层时才能拾取。

### 3. 提交点（特定图层提交、按 E 交互）

| 挂载 | 配置 |
|------|------|
| **父物体** | 只挂 **LayerSign**：**Layer** = 提交点所在图层，**Follow** 不勾选。 |
| **子物体** | **LayerSubmitZone**：**Layer Id** = 0/1/2/3。**Required Item Id** 与对应 LayerCollectible 的 **Item Id** 一致，**Faction Id** 按需求填。**Collider2D（Is Trigger）**、**Rigidbody2D（Kinematic）** 需有。 |

- 显隐由父物体 **LayerSign** 控制；只有玩家在该图层且携带对应物品时才能在此提交。

### 4. 全图层都显示（不随图层显隐）

- 挂 **LayerSign**，勾选 **Follow** 即可，Layer 可任意。适合 UI、全图层可见的机关等。

### 对应关系小结

| MapLayer（LayerSign） | Layer Id（LayerCollectible / LayerSubmitZone） |
|-----------------------|------------------------------------------------|
| Main                  | 0                                              |
| Layer1                | 1                                              |
| Layer2                | 2                                              |
| Layer3                | 3                                              |

- 布景“只显示在当前图层”＝父物体挂 **LayerSign** 且 **Follow** 不勾选。  
- 布景“只能在该图层交互”＝子物体挂 **LayerCollectible** 或 **LayerSubmitZone**，**Layer Id** 与 LayerSign 的图层一致；图层切换由 **LayerMapClient** 同步到 **LayerTaskManager**，无需新增脚本。
