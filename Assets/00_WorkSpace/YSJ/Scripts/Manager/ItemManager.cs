using System.Collections.Generic;
using UnityEngine;
using YSJ.Util;

namespace YSJ
{
    public class ItemManager : SimpleSingletonPun<ItemManager>
    {
        private bool _isLoadedItem = false;

        [Header("Load Config")]
        [SerializeField] private bool _useLoadAllItem = true;
        [SerializeField] private List<UnimoItemSO> _itemSOList = new();

        [Header("Item Box")]
        [SerializeField] private List<ItemBox> _thisSceneItemBoxList = new();

        protected override void Init()
        {
            base.Init();

            LoadAllItem();
            LoadThisSceneItemBox();
        }

        private void LoadAllItem()
        {
            _isLoadedItem = true;

            if (!_useLoadAllItem) return;
            this.PrintLog("LoadAllItem 진행");

            // 초기화 시, 해당 경로의 필요 아이템들을 로드합니다.
            UnimoItemSO[] loadItemSOArray = Resources.LoadAll<UnimoItemSO>(LoadPath.PLAYER_UNIMO_ITEM_PATH);

            if (loadItemSOArray.Length > 0)
            {
                foreach (var itemSO in loadItemSOArray)
                {
                    if (_itemSOList.Contains(itemSO))
                    {
                        this.PrintLog($"모든 아이템 자동 로드 시, {itemSO}은 List에 이미 포함 되어 있습니다.", LogType.Warning);
                        continue;
                    }

                    this.PrintLog($"모든 아이템 자동 로드 시, {itemSO}은 List에 추가합니다.");
                    _itemSOList.Add(itemSO);
                }
            }
            else
            {
                this.PrintLog($"모든 아이템 자동 로드를 진행 할 수 있는 아이템이 없습니다.", LogType.Warning);
            }
            
            this.PrintLog("LoadAllItem 진행 완료");
        }

        private void LoadThisSceneItemBox()
        {
            var itemBoxs = GameObject.FindObjectsOfType<ItemBox>();
            foreach (var itemBox in itemBoxs)
            {
                if (_thisSceneItemBoxList.Contains(itemBox))
                    continue;


            }
        }

        public UnimoItemSO[] GetItemSOs()
        {
            if (!_isLoadedItem) LoadAllItem();
            if(_itemSOList == null) return null;

            return _itemSOList.ToArray();
        }
    }
}