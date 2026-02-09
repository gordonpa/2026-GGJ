using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Netcode;

/// <summary>
/// 使用 SerializedObject/SerializedProperty 修复 NetworkManager 中丢失的 NetworkPrefabsList 引用，
/// 确保修改正确写入场景并持久化。
/// </summary>
public class NetworkPrefabsFixer : EditorWindow
{
    /// <summary>从 NetworkManager 的 SerializedObject 中找到 NetworkPrefabsLists 属性。</summary>
    static SerializedProperty FindNetworkPrefabsListsProperty(SerializedObject so)
    {
        // 先尝试完整路径（部分 Unity 版本支持）
        SerializedProperty lists = so.FindProperty("NetworkConfig.Prefabs.NetworkPrefabsLists");
        if (lists != null)
            return lists;
        lists = so.FindProperty("m_NetworkConfig.Prefabs.NetworkPrefabsLists");
        if (lists != null)
            return lists;

        // 逐级查找
        SerializedProperty config = so.FindProperty("NetworkConfig");
        if (config == null)
            config = so.FindProperty("m_NetworkConfig");
        if (config == null)
            return null;

        SerializedProperty prefabs = config.FindPropertyRelative("Prefabs");
        if (prefabs == null)
            prefabs = config.FindPropertyRelative("m_Prefabs");
        if (prefabs == null)
            return null;

        lists = prefabs.FindPropertyRelative("NetworkPrefabsLists");
        if (lists == null)
            lists = prefabs.FindPropertyRelative("m_NetworkPrefabsLists");
        return lists;
    }

    [MenuItem("Tools/Netcode/修复并保存场景")]
    public static void FixAndSaveScene()
    {
        FixNetworkPrefabsReferences();
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.IsValid() && scene.isDirty)
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("场景已保存。");
        }
    }

    [MenuItem("Tools/Netcode/修复 NetworkPrefabsList 引用")]
    public static void FixNetworkPrefabsReferences()
    {
        // 查找所有 NetworkManager（包括未启用的）
        NetworkManager[] networkManagers = Object.FindObjectsOfType<NetworkManager>(true);
        
        if (networkManagers.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "场景中没有找到 NetworkManager！", "确定");
            return;
        }

        // 查找 DefaultNetworkPrefabs
        string[] guids = AssetDatabase.FindAssets("t:NetworkPrefabsList");
        NetworkPrefabsList defaultList = null;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            NetworkPrefabsList list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(path);
            if (list != null && list.name == "DefaultNetworkPrefabs")
            {
                defaultList = list;
                break;
            }
        }

        if (defaultList == null)
        {
            defaultList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
            if (defaultList == null)
            {
                EditorUtility.DisplayDialog("错误", 
                    "找不到 DefaultNetworkPrefabs.asset 文件！\n\n" +
                    "请确保该文件存在于 Assets/ 目录下，或者手动在 NetworkManager 中配置 NetworkPrefabsList。", 
                    "确定");
                return;
            }
        }

        int fixedCount = 0;
        int enabledCount = 0;
        bool sceneDirty = false;

        foreach (NetworkManager manager in networkManagers)
        {
            SerializedObject so = new SerializedObject(manager);

            // 启用组件：通过 SerializedObject 修改，以便持久化
            SerializedProperty enabledProp = so.FindProperty("m_Enabled");
            if (enabledProp != null && enabledProp.intValue == 0)
            {
                enabledProp.intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"已启用 NetworkManager '{manager.name}' 组件。");
                enabledCount++;
                sceneDirty = true;
            }

            SerializedProperty listsProp = FindNetworkPrefabsListsProperty(so);
            if (listsProp == null)
            {
                Debug.LogWarning($"NetworkManager '{manager.name}'：无法找到 NetworkPrefabsLists 的 SerializedProperty，请检查 Netcode 包版本。");
                continue;
            }

            so.Update();
            listsProp = FindNetworkPrefabsListsProperty(so);
            if (listsProp == null)
                continue;

            int size = listsProp.arraySize;
            bool needsFix = false;

            // 从后往前删除 null 元素，避免索引错位
            for (int i = size - 1; i >= 0; i--)
            {
                SerializedProperty element = listsProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                {
                    Debug.LogWarning($"NetworkManager '{manager.name}' 的 NetworkPrefabsLists[{i}] 为 null，已通过 SerializedProperty 移除。");
                    listsProp.DeleteArrayElementAtIndex(i);
                    needsFix = true;
                }
            }

            bool addedDefault = false;
            // 若列表为空或刚删除了 null，确保有默认列表
            if (needsFix || listsProp.arraySize == 0)
            {
                bool hasDefault = false;
                for (int i = 0; i < listsProp.arraySize; i++)
                {
                    if (listsProp.GetArrayElementAtIndex(i).objectReferenceValue == defaultList)
                    {
                        hasDefault = true;
                        break;
                    }
                }
                if (!hasDefault)
                {
                    listsProp.arraySize++;
                    listsProp.GetArrayElementAtIndex(listsProp.arraySize - 1).objectReferenceValue = defaultList;
                    Debug.Log($"已为 NetworkManager '{manager.name}' 添加 DefaultNetworkPrefabs 引用（SerializedProperty）。");
                    fixedCount++;
                    addedDefault = true;
                }
            }

            bool modified = needsFix || addedDefault;
            if (modified)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(manager);
                sceneDirty = true;
            }
        }

        if (sceneDirty)
        {
            var scene = networkManagers[0].gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        if (fixedCount > 0 || enabledCount > 0)
        {
            string message = "修复完成：\n";
            if (enabledCount > 0)
                message += $"• 已启用 {enabledCount} 个 NetworkManager 组件\n";
            if (fixedCount > 0)
                message += $"• 已修复 {fixedCount} 个 NetworkManager 的 NetworkPrefabsList 引用（已写入场景）\n";
            message += "\n请按 Ctrl+S 保存场景后重新运行游戏。";
            EditorUtility.DisplayDialog("修复完成", message, "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("检查完成", 
                "所有 NetworkManager 的配置都是有效的，且组件已启用。", 
                "确定");
        }
    }

    [MenuItem("Tools/Netcode/检查 NetworkPrefabsList 引用")]
    public static void CheckNetworkPrefabsReferences()
    {
        NetworkManager[] networkManagers = FindObjectsOfType<NetworkManager>();
        
        if (networkManagers.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "场景中没有找到 NetworkManager！", "确定");
            return;
        }

        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("NetworkPrefabsList 引用检查报告：\n");

        foreach (NetworkManager manager in networkManagers)
        {
            report.AppendLine($"NetworkManager: {manager.name}");
            
            // 检查组件是否启用
            if (!manager.enabled)
            {
                report.AppendLine("  ⚠️  组件已禁用（需要启用）");
            }
            else
            {
                report.AppendLine("  ✅ 组件已启用");
            }
            
            if (manager.NetworkConfig == null)
            {
                report.AppendLine("  ❌ NetworkConfig 为 null");
                continue;
            }

            if (manager.NetworkConfig.Prefabs == null)
            {
                report.AppendLine("  ❌ NetworkConfig.Prefabs 为 null");
                continue;
            }

            var lists = manager.NetworkConfig.Prefabs.NetworkPrefabsLists;
            report.AppendLine($"  NetworkPrefabsLists 数量: {lists.Count}");

            bool hasNull = false;
            for (int i = 0; i < lists.Count; i++)
            {
                if (lists[i] == null)
                {
                    report.AppendLine($"  ❌ NetworkPrefabsLists[{i}] 为 null（丢失引用）");
                    hasNull = true;
                }
                else
                {
                    report.AppendLine($"  ✅ NetworkPrefabsLists[{i}]: {lists[i].name}");
                    
                    // 检查列表中的预制体
                    int prefabCount = lists[i].PrefabList.Count;
                    int nullPrefabCount = 0;
                    foreach (var prefab in lists[i].PrefabList)
                    {
                        if (prefab == null || prefab.Prefab == null)
                        {
                            nullPrefabCount++;
                        }
                    }
                    
                    if (nullPrefabCount > 0)
                    {
                        report.AppendLine($"    ⚠️  预制体引用: {prefabCount - nullPrefabCount}/{prefabCount} 有效，{nullPrefabCount} 个丢失");
                    }
                    else
                    {
                        report.AppendLine($"    ✅ 预制体引用: {prefabCount} 个全部有效");
                    }
                }
            }

            if (!hasNull && lists.Count > 0)
            {
                report.AppendLine("  ✅ 所有引用有效");
            }
            
            report.AppendLine();
        }

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("检查完成", report.ToString(), "确定");
    }
}

