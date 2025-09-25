using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YSJ.Util;

namespace YSJ
{
    [DefaultExecutionOrder(-50)]
    public class ItemManager : SimpleSingletonPun<ItemManager>
    {
        private readonly List<ItemSpawnProbabilityData> _globalPool = new();
        private readonly Dictionary<ItemId, UnimoItemSO> _idIndex = new();

        protected override void Init()
        {
            base.Init();
        }

        public void RegisterItemDatas(params ItemSpawnProbabilityData[] arr)
        {
            if (arr == null || arr.Length == 0) return;

            foreach (var d in arr)
            {
                if (d == null || d.Item == null) continue;

                _globalPool.Add(d);

                var id = d.Item.itemID;
                if (id != ItemId.None)
                    _idIndex[id] = d.Item; // 최신 참조로 갱신
            }

#if UNITY_EDITOR
            this.PrintLog($"Registered: pool={_globalPool.Count}, ids={_idIndex.Count}");
#endif
        }

        public UnimoItemSO ResolveById(ItemId id)
        {
            if (id == ItemId.None) return null;
            if (_idIndex.TryGetValue(id, out var so) && so != null) return so;

            // 없으면 로드를 할까, 말까

            return null;
        }

        public UnimoItemSO GetRandomItemSO()
        {
            return WeightedPick(_globalPool);
        }

        public UnimoItemSO GetRandomItemSO(List<ItemSpawnProbabilityData> localPool)
        {
            return WeightedPick(localPool);
        }

        private UnimoItemSO WeightedPick(List<ItemSpawnProbabilityData> pool)
        {
            if (pool == null || pool.Count == 0) return null;

            var list = pool.Where(p => p != null && p.Item != null && p.Weight > 0f).ToList();
            if (list.Count == 0) return null;

            float total = 0f;
            foreach (var p in list) total += p.Weight;

            float r = UnityEngine.Random.Range(0f, total);
            float acc = 0f;
            foreach (var p in list)
            {
                acc += p.Weight;
                if (r <= acc) return p.Item;
            }
            return list[list.Count - 1].Item;
        }
    }
}
