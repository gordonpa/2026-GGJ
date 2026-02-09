using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LayerMap
{
    /// <summary>
    /// 图层标记，添加此脚本的物体会被标记为图层物体。
    /// 自动控制【子节点】的显隐。
    /// 可以手动在OwnedObjs添加节点，会一同控制
    /// </summary>
    public class LayerSign : MonoBehaviour
    {
        /// <summary>
        /// 所拥有的物品列表，将会控制这些物品的显隐
        /// </summary>
        public List<Transform> OwnedObjs = new List<Transform>();
        public MapLayer Layer = MapLayer.Main;
        [Header("是否跟随玩家前往目标图层")]
        public bool Follow;
        private void Awake()
        {
            for (int i = 0; i < this.transform.childCount; i++)
            {
                OwnedObjs.Add(this.transform.GetChild(i));
            }
        }

        public void ShowOrHide(bool show)
        {
            // 无子节点时：控制自身显隐（布景挂在同一物体上时生效）
            if (OwnedObjs == null || OwnedObjs.Count == 0)
            {
                gameObject.ChangeActive(show);
                return;
            }
            foreach (Transform obj in OwnedObjs)
            {
                obj.gameObject.ChangeActive(show);
            }
        }
    }
}
