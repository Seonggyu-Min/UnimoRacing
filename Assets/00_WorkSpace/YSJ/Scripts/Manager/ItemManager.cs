using System;
using System.Collections.Generic;
using UnityEngine;
using YSJ.Util;

namespace YSJ
{
    public class ItemManager : SimpleSingletonPun<ItemManager>
    {

        [Header("Load Config")]
        [SerializeField] private bool _useLoadAllItem = true;
        [SerializeField] private List<ItemSpawnProbabilityData> _itemDataList = new();

        [Header("Item Box")]
        [SerializeField] private List<ItemBox> _thisSceneItemBoxList = new();

        public Action OnInitAction;

        private bool _isLoadedItem = false;

        protected override void Init()
        {
            base.Init();

            LoadAllItem();
            LoadThisSceneItemBox();
            OnInitAction?.Invoke();
        }

        #region Load 관련
        private void LoadAllItem()
        {
            if (!_useLoadAllItem) return;
            if (_isLoadedItem) return;

            this.PrintLog("LoadAllItem 진행");

            // 초기화 시, 해당 경로의 필요 아이템들을 로드합니다.
            UnimoItemSO[] loadItemSOArray = Resources.LoadAll<UnimoItemSO>(LoadPath.PLAYER_UNIMO_ITEM_PATH);

            if (loadItemSOArray.Length > 0)
            {
                foreach (var itemSO in loadItemSOArray)
                {
                    if (ContainsUnimoSO(itemSO))
                    {
                        this.PrintLog($"모든 아이템 자동 로드 시, {itemSO}은 List에 이미 포함 되어 있습니다.", LogType.Warning);
                        continue;
                    }

                    this.PrintLog($"모든 아이템 자동 로드 시, {itemSO}은 List에 추가합니다.");

                    ItemSpawnProbabilityData addData = new ItemSpawnProbabilityData();
                    addData.itemSO = itemSO;
                    addData.spawnProbability = 1.0f;

                    _itemDataList.Add(addData);
                }
            }
            else
            {
                this.PrintLog($"모든 아이템 자동 로드를 진행 할 수 있는 아이템이 없습니다.", LogType.Warning);
            }

            this.PrintLog("LoadAllItem 진행 완료");

            _isLoadedItem = true;
        }
        private void LoadThisSceneItemBox()
        {
            var itemBoxs = GameObject.FindObjectsOfType<ItemBox>();
            foreach (var itemBox in itemBoxs)
            {
                if (itemBox == null)
                {
                    this.PrintLog($"아이템 박스");
                    continue;
                }

                if (_thisSceneItemBoxList.Contains(itemBox))
                    continue;

                _thisSceneItemBoxList.Add(itemBox);
            }
        }

        #endregion

        #region Register
        public void RegisterItemData(ItemSpawnProbabilityData inData, bool forceRegister = false)
        {
            bool isContainsSO = ContainsUnimoSO(inData.itemSO);
            if (isContainsSO)
            {
                this.PrintLog($"포합된 SO가 존재하여 해당 {inData} 등록은 보류됩니다.");
                if (forceRegister)
                {
                    this.PrintLog($"보류된 {inData} 등록의 spawnProbability 값만 현 Data로 변경됩니다.");

                    foreach (var data in _itemDataList)
                    {
                        if (inData.itemSO.itemID.Equals(data.itemSO.itemID))
                        {
                            data.spawnProbability = inData.spawnProbability;
                            break;
                        }
                    }
                }
                return;
            }

            _itemDataList.Add(inData);
        }
        public void RegisterItemDatas(params ItemSpawnProbabilityData[] inDatas)
        {
            foreach (var inData in inDatas)
            {
                if (inData == null) continue;
                RegisterItemData(inData);
            }
        }

        #endregion

        #region 반환 관련
        /// <summary>
        /// 포함 여부 확인
        /// </summary>
        /// <param name="so"></param>
        /// <returns></returns>
        public bool ContainsUnimoSO(UnimoItemSO so)
        {
            if (so == null || _itemDataList.Count <= 0) return false;

            foreach (var data in _itemDataList)
            {
                // 데이터가 없으면
                if (data == null) continue;

                // 데이터에 비교 대상이랑 맞으면
                if (data.itemSO == so) return true;
            }

            // 찾을 수 없으면
            return false;
        }

        /// <summary>
        ///  아이템 SO 들 리턴(중복 제거 리스트) List
        /// </summary>
        /// <returns></returns>
        public List<UnimoItemSO> GetItemSOs()
        {
            LoadAllItem();

            List<UnimoItemSO> resultList = new();
            foreach (var data in _itemDataList)
            {
                // 데이터가 없으면
                if (data == null) continue;

                // 데이터 안에, so가 없다면
                var so = data.itemSO;
                if (so == null) continue;

                // 리턴해줄 리스트에 so가 포함 되어 있다면
                if (resultList.Contains(so)) continue;

                // 추가
                resultList.Add(data.itemSO);
            }
            return resultList;
        }

        /// <summary>
        /// 아이템 스폰 데이터 List
        /// </summary>
        /// <returns></returns>
        public List<ItemSpawnProbabilityData> GetItemSpawnProbabilityDataList()
        {
            LoadAllItem();
            return _itemDataList;
        }

        // 반환: 랜덤 아이템 SO(가중치 랜덤 진행 후)
        public UnimoItemSO GetRandomItemSO()
        {
            GetItemSpawnProbabilityDataList();

            UnimoItemSO result = null;
            if (_itemDataList.Count <= 0) return result;

            float catMax = 0.0f;

            // catMax 도출
            for (int i = 0; i < _itemDataList.Count; i++)
            {
                ItemSpawnProbabilityData data = _itemDataList[i];
                if (data == null) continue;

                catMax += data.spawnProbability;
            }

            float random = UnityEngine.Random.Range(0, catMax);
            float nextCat = 0.0f;

            // 컷 체크 및 SO 선별
            for (int i = 0; i < _itemDataList.Count; i++)
            {
                ItemSpawnProbabilityData data = _itemDataList[i];
                if (data == null) continue;

                if (random > nextCat + data.spawnProbability)
                {
                    nextCat += data.spawnProbability;
                    continue;
                }

                result = data.itemSO;
                break;
            }

            return result;
        }

        public UnimoItemSO GetRandomItemSO(List<ItemSpawnProbabilityData> itemDataList)
        {
            GetItemSpawnProbabilityDataList();

            UnimoItemSO result = null;
            if (itemDataList.Count <= 0) return result;

            float catMax = 0.0f;

            // catMax 도출
            for (int i = 0; i < itemDataList.Count; i++)
            {
                ItemSpawnProbabilityData data = itemDataList[i];
                if (data == null) continue;

                catMax += data.spawnProbability;
            }

            float random = UnityEngine.Random.Range(0, catMax);
            float nextCat = 0.0f;

            // 컷 체크 및 SO 선별
            for (int i = 0; i < itemDataList.Count; i++)
            {
                ItemSpawnProbabilityData data = itemDataList[i];
                if (data == null) continue;

                if (random > nextCat + data.spawnProbability)
                {
                    result = data.itemSO;
                    break;
                }
                nextCat += data.spawnProbability;
            }

            return result;
        }

        #endregion
    }
}