using System.Collections.Generic;
using UnityEngine;
using YSJ.Util;

public class ItemRegistry : SimpleSingleton<ItemRegistry>
{
    private bool _isInit = false;

    [SerializeField] private bool _useLoadAllItem = false;

    [SerializeField]
    private List<UnimoItemSO> _items = new();


    protected override void Init()
    {
        base.Init();

        LoadAllItem();

        _isInit = true;
    }

    private void LoadAllItem()
    {
        if (!_useLoadAllItem) return;

        this.PrintLog("LoadAllItem 진행");

        // 초기화 시, 해당 경로의 필요 아이템들을 로드합니다.
        UnimoItemSO[] loadItemSOArray = Resources.LoadAll<UnimoItemSO>(LoadPath.PLAYER_UNIMO_ITEM_PATH);

        if (loadItemSOArray.Length > 0)
        {
            foreach (var itemSO in loadItemSOArray)
            {
                if (_items.Contains(itemSO))
                {
                    this.PrintLog($"모든 아이템 자동 로드 시, {itemSO}은 List에 이미 포함 되어 있습니다.", LogType.Warning);
                    continue;
                }

                this.PrintLog($"모든 아이템 자동 로드 시, {itemSO}은 List에 추가합니다.");
                _items.Add(itemSO);
            }
        }
        else
        {
            this.PrintLog($"모든 아이템 자동 로드를 진행 할 수 있는 아이템이 없습니다.", LogType.Warning);
        }


        this.PrintLog("LoadAllItem 진행 완료");
    }
}
