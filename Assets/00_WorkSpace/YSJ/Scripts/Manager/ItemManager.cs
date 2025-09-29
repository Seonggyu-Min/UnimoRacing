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
            Setup();
        }


        #region Setup
        public bool Setup()
        {
            SetupGameItemSpawnProbabilitySO();
            SetupItemBoxs();
            return _isSetupSpawnProbability && _isSetupItemBoxs;
        }
        private void SetupGameItemSpawnProbabilitySO()
        {
            // 셋업 여부가 중요하지 않음
            _isSetupSpawnProbability = true;

            if (_spawnProbabilitySO == null)
            {
                this.PrintLog($"SpawnProbabilitySO 가 존재하지 않습니다.");
                return;
            }

            ItemSpawnProbabilityData[] list = _spawnProbabilitySO.probabilityList.ToArray();
            RegisterItems(list);
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

        #endregion

        #region Register
        public void RegisterItem(ItemSpawnProbabilityData data)
        {
            if (data == null)
            {
                this.PrintLog($"RegisterItem 실패: data == null");
                return;
            }

            if (data.Item == null)
            {
                this.PrintLog($"RegisterItem 실패: data.Item == null");
                return;
            }
            
            if (_globalPool.Any(x => x.Item != null && x.Item.itemID == data.Item.itemID))
            {
                this.PrintLog($"RegisterItem 실패: 동일한 아이템이 존재합니다. 요청 Item: {data.Item.itemName}({data.Item.itemID})");
                return;
            }

            _globalPool.Add(data);
            _idIndex.Add(data.Item.itemID, data.Item);
        }
        public void RegisterItems(params ItemSpawnProbabilityData[] setList)
        {
            if (setList == null)
            {
                this.PrintLog($"다중 RegisterItem 실패: setList == null");
                return;
            }

            if (setList.Length <= 0)
            {
                this.PrintLog($"다중 RegisterItem 실패: 등록할 아이템 List가 없음.");
                return;
            }

            for (int i = 0; i < setList.Length; i++)
            {
                ItemSpawnProbabilityData checkData = setList[i];
                RegisterItem(checkData);
            }
        }

        #endregion

        #region UnimoItemSO
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

        #endregion
    }
}
