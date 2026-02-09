using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace Network
{
    public partial class Server
    {
        public NetworkVariable<long> Timestamp = new NetworkVariable<long>();


        public DateTime Time { get; private set; }
        public Action OnTimeTick;

        public override void OnNetworkSpawn()
        {
            SynTime();
            Timestamp.OnValueChanged += OnTimeChange;
        }

        public override void OnNetworkDespawn()
        {
            Timestamp.OnValueChanged -= OnTimeChange;
        }

        public void OnTimeChange(long oldValue, long newValue)
        {
            SynTime();
        }

        public void SynTime()
        {
            Time = new DateTime(Timestamp.Value);
            time = 0;
            Debug.Log($"[Server] 同步时间：{Time.ToString()}");
        }

        /// <summary>
        /// 同步时间戳，在时间敏感的场景可以同步后再开始计时
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SynTimestampServerRpc()
        {
            Timestamp.Value = System.DateTime.Now.Ticks;
        }

        float time = 0;
        private void Update()
        {
            time += UnityEngine.Time.deltaTime;
            if (Time != null && time >= 1)
            {
                time = 0;
                Time = Time.AddSeconds(1);
                OnTimeTick?.Invoke();
            }
        }
    }
}
