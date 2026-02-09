# LayerMapClient 预制体说明

## 当前项目状态

项目中**已有**预制体：`Assets/Resources/LayerMapClient.prefab`  
且已加入 `DefaultNetworkPrefabs`，服务器可通过 `Resources.Load<LayerMapClient>("LayerMapClient")` 加载并生成。  
**一般不需要新建**，只需确保场景里挂了 **LayerMapClientBootstrap**，让客户端自动请求生成。

---

## 若需要从零新建预制体

按下面步骤做即可。

### 1. 新建空物体

- Hierarchy 右键 → Create Empty
- 命名为 **LayerMapClient**

### 2. 挂载的组件（顺序无所谓）

| 组件 | 说明 |
|------|------|
| **NetworkObject** | Netcode for GameObjects 的联网对象，用于 `Spawn` / 同步。必须挂。 |
| **LayerMapClient** | 脚本：存当前图层 `Layer`、名字 `Name`，提供 `GotoLayer()`。 |
| **LayerMapSync** | 脚本：根据当前图层显隐场景里的 `LayerSign`。`LayerMapClient` 带 `[RequireComponent(typeof(LayerMapSync))]`，加 LayerMapClient 时会自动要求有 LayerMapSync。 |

即：**NetworkObject + LayerMapClient + LayerMapSync**（后两个脚本在同一物体上即可）。

### 3. 保存为预制体并放到 Resources

1. 将 **LayerMapClient** 物体从 Hierarchy **拖到** `Assets/Resources/` 下，生成预制体。
2. 预制体名称必须是 **LayerMapClient**（和 `Resources.Load<LayerMapClient>("LayerMapClient")` 的字符串一致）。
3. 删除 Hierarchy 里刚才的实例（只保留 Resources 里的预制体即可）。

### 4. 注册到 NetworkManager

1. 打开 **NetworkManager**（或 **NetworkManagerEx**）所在场景。
2. 在 Inspector 的 **NetworkManager** → **Network Config** → **Prefabs** 里，把 **Network Prefabs List** 指向你的列表（如 `DefaultNetworkPrefabs`）。
3. 打开 **DefaultNetworkPrefabs**（或你用的那个 List），在 **List** 里点 **+**，把 `Assets/Resources/LayerMapClient` 预制体拖进去并保存。

### 5. 场景里要有 LayerMapClientBootstrap

在**有 LayerMapManager 的场景**里，给任意物体挂上 **LayerMapClientBootstrap**，这样客户端有玩家后会自动调用 `ReqGenClient()`，服务器才会为每个玩家生成 LayerMapClient，否则 Client 会一直为 N、AllClient 为 0。

---

## 小结

- **已有预制体**：直接用，确认场景有 **LayerMapClientBootstrap** 即可。
- **从零建**：空物体 → 挂 **NetworkObject + LayerMapClient + LayerMapSync** → 拖到 `Resources`、命名为 **LayerMapClient** → 加入 NetworkManager 的 Prefabs List。
