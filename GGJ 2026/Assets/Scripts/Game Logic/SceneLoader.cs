using UnityEngine;
using Unity.Netcode;  // 添加 NGO 命名空间
using UnityEngine.SceneManagement;

public class SceneLoader : NetworkBehaviour  // 改为继承 NetworkBehaviour
{
    // 通过场景名称加载
    public void LoadNetworkSceneByName(string sceneName)
    {
        // 只有服务端/主机才能切换场景
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("只有服务端或主机可以切换场景");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // 通过场景索引加载
    public void LoadNetworkSceneByIndex(int sceneIndex)
    {
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("只有服务端或主机可以切换场景");
            return;
        }

        string sceneName = SceneManager.GetSceneByBuildIndex(sceneIndex).name;
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // 客户端请求服务器加载场景（通过 ServerRpc）
    [ServerRpc(RequireOwnership = false)]
    public void RequestLoadSceneServerRpc(string sceneName)
    {
        LoadNetworkSceneByName(sceneName);
    }
}