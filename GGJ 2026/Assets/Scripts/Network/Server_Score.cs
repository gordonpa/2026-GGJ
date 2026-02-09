using LayerMap;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
namespace Network
{
    public partial class Server
    {
        /// <summary>
        /// 请求修改昵称
        /// </summary>
        /// <param name="clientId">客户端Id</param>
        [ServerRpc(RequireOwnership = false)]
        public void SetNameServerRpc(ulong clientId, FixedString64Bytes name)
        {
            if (LayerMapManager.Instance.TryGetClient(clientId, out var client))
            {
                client.PlayerName.Value = name;
                Debug.Log($"[Server] 客户端:{client.OwnerClientId} 设定名称：{client.PlayerName.Value}");
            }
            else
            {
                Debug.LogError($"[Server] 无法找到客户端数据");
            }
        }
        /// <summary>
        /// 记录分数变化次数
        /// </summary>
        public NetworkVariable<int> ScoreChangeRecordCount = new NetworkVariable<int>();
        /// <summary>
        /// 请求增加分数（默认 +5，供测试等调用）
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AddScoreServerRpc(ulong clientId)
        {
            AddScoreServerRpc(clientId, 5);
        }

        /// <summary>
        /// 请求增加分数（指定分数，供提交点等调用）
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AddScoreServerRpc(ulong clientId, int points)
        {
            if (points <= 0) return;
            if (LayerMapManager.Instance.TryGetClient(clientId, out var client))
            {
                client.Score.Value += points;
                ScoreChangeRecordCount.Value++;
                Debug.Log($"[Server] 客户端:{client.OwnerClientId} 增加分数 +{points}");
            }
            else
            {
                Debug.LogError($"[Server] 无法找到客户端数据 clientId={clientId}，请确保场景有 LayerMapManager 且已为玩家生成 LayerMapClient（挂 LayerMapClientBootstrap）");
            }
        }
    }
}
