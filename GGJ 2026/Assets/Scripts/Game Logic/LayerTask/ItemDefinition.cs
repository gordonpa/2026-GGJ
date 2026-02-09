using UnityEngine;

[CreateAssetMenu(fileName = "Item_Dice", menuName = "GameJam/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Tooltip("0=骰子,1=沙漏,2=晶石,3=钥匙")]
    public int itemId;
    public string itemName;
    public GameObject prefab;  // 不是 LayerCollectible，不是 Transform
    public Sprite icon;                // UI用
    public bool canCarryCrossLayer;    // 能否跨图层携带
}