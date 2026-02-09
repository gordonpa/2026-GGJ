using UnityEngine;
using System.Collections.Generic;
using LayerMap;

namespace GameJam.LayerTask
{
    [CreateAssetMenu(fileName = "SpawnProfile", menuName = "GameJam/Spawn Profile")]
    public class SpawnProfile : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public ItemDefinition item;
            public DistributionMode mode;
            public MapLayer[] specificLayers;
            public int countPerLayer = 1;
        }

        public enum DistributionMode { AllLayersEqual, SpecificLayersOnly }

        [Header("生成规则")]
        public Entry[] entries;

        // 返回每层要生成什么物品
        public Dictionary<MapLayer, List<ItemDefinition>> GetLayerTasks()
        {
            var result = new Dictionary<MapLayer, List<ItemDefinition>>();
            var allLayers = new[] { MapLayer.Main, MapLayer.Layer1, MapLayer.Layer2, MapLayer.Layer3 };

            foreach (var l in allLayers) result[l] = new List<ItemDefinition>();

            foreach (var entry in entries)
            {
                var layers = entry.mode == DistributionMode.AllLayersEqual ? allLayers : entry.specificLayers;
                foreach (var layer in layers)
                    for (int i = 0; i < entry.countPerLayer; i++)
                        result[layer].Add(entry.item);
            }
            return result;
        }
    }
}