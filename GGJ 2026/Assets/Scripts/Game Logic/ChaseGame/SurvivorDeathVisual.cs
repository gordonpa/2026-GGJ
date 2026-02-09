using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 求生者死亡后隐藏外观（可选指定 visualRoot 或隐藏所有子物体 SpriteRenderer）。挂玩家预制体。
/// </summary>
[RequireComponent(typeof(FactionMember))]
public class SurvivorDeathVisual : NetworkBehaviour
{
    [Header("隐藏方式")]
    [Tooltip("若指定则死亡时 SetActive(false)，否则隐藏所有子物体 SpriteRenderer")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("未指定 visualRoot 时，是否包含自身 SpriteRenderer")]
    [SerializeField] private bool includeSelf = true;

    private FactionMember faction;
    private SurvivorState state;
    private bool lastDead;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        faction = GetComponent<FactionMember>();
        state = GetComponent<SurvivorState>();
    }

    private void Update()
    {
        if (state == null || !faction.IsSurvivor) return;
        bool dead = state.IsDead;
        if (dead == lastDead) return;
        lastDead = dead;
        ApplyVisible(!dead);
    }

    private void ApplyVisible(bool visible)
    {
        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(visible);
            return;
        }
        if (includeSelf)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = visible;
        }
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = visible;
    }
}
