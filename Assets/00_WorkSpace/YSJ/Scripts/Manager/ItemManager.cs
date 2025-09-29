using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YSJ.Util;

namespace YSJ
{
    [DefaultExecutionOrder(-50)]
    public class ItemManager : SimpleSingletonPun<ItemManager>, IGameSetup
    {
        [Header("IGameSetup Config")]
        [SerializeField] private int _order = 0;

        [Header("Item Config")]
        [SerializeField] private GameItemSpawnProbabilitySO _spawnProbabilitySO;

        // 개별 셋업
        private bool _isSetupSpawnProbability;
        private bool _isSetupItemBoxs;

        private readonly List<ItemSpawnProbabilityData> _globalPool = new();
        private readonly Dictionary<ItemId, UnimoItemSO> _idIndex = new();

        private List<ItemBox> _registeredItemBoxes = new();



        public int Order => _order;


        
        protected override void Init()
        {
            base.Init();

            SetupGameItemSpawnProbabilitySO();
            SetupItemBoxs();
        }



        private void SetupGameItemSpawnProbabilitySO()
        {
            // SO가 있다면
            if (_spawnProbabilitySO != null && _spawnProbabilitySO.probabilityList.Count >= 1)
            {
                List<ItemSpawnProbabilityData> list = _spawnProbabilitySO.probabilityList;
                for (int i = 0; i < list.Count; i++)
                {
                    ItemSpawnProbabilityData checkData = list[i];

                    if (_globalPool.Any(x => x.Item != null && checkData.Item != null &&
                                             x.Item.itemID == checkData.Item.itemID))
                        continue;

                    _globalPool.Add(checkData);
                    _idIndex.Add(checkData.Item.itemID, checkData.Item);
                }
            }

            _isSetupSpawnProbability = true;
        }
        private void SetupItemBoxs()
        {
            ItemBox[] items = FindObjectsOfType<ItemBox>();
            foreach (var item in items)
            {
                if (item == null)
                    continue;

                if (!item.IsSetup)
                    item.Setup();

                if (!_registeredItemBoxes.Contains(item))
                    _registeredItemBoxes.Add(item);
            }

            _isSetupItemBoxs = true;
        }



        public bool Setup()
        {
            return _isSetupSpawnProbability && _isSetupItemBoxs;
        }



        public UnimoItemSO ResolveById(ItemId id)
        {
            if (id == ItemId.None) return null;
            if (_idIndex.TryGetValue(id, out var so) && so != null) return so;

            return null;
        }
        public UnimoItemSO GetRandomOneItemSO()
        {
            return WeightedPick(_globalPool);
        }
        public UnimoItemSO GetRandomOneItemSO(List<ItemSpawnProbabilityData> localPool)
        {
            return WeightedPick(localPool);
        }
        private UnimoItemSO WeightedPick(List<ItemSpawnProbabilityData> pool)
        {
            if (pool == null || pool.Count == 0) return null;

            var list = pool.Where(
                p => p != null
                && p.Item != null
                && p.Weight > 0f).ToList();

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
