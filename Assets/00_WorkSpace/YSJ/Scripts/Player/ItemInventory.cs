using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using YSJ.Util;

public class ItemInventory : MonoBehaviour
{
    #region Parameter
    // Const
    private const int DEFAULT_INVENTORY_MAX_COUNT = 2;

    // SerializeField
    [Header("Config")]
    [SerializeField] private bool _selfSetup = false;
    [SerializeField] private int _inventroySaveMaxCount = 2;

    // publilc Action

    // private Parameter
    private bool _isSetup = false;

    private PhotonView _ownerView;
    private PlayerRaceData _data;

    private UnimoItemSO[] _items;
    private List<UnimoItemSO> _delaySaveItemList = new();

    // public Ram
    public bool IsSetup => _isSetup;

    #endregion

    public void Awake()
    {
        if (_selfSetup)
            Setup(null);
    }

    public void Setup(PlayerRaceData data = null)
    {
        if (_isSetup)
        {
            this.PrintLog("셋업이 되어 있는 상태에서, 다시 시도 하여 반려됩니다.");
            return;
        }
        _ownerView = GetComponentInParent<PhotonView>();
        _data = data;

        int itemsLength = (_inventroySaveMaxCount > 0) ? _inventroySaveMaxCount : DEFAULT_INVENTORY_MAX_COUNT;

        _items = new UnimoItemSO[itemsLength];

        _isSetup = (_selfSetup || _data != null);
    }

    public void SaveItem(UnimoItemSO itemSO, bool isDelaySave = false)
    {
        if (!_isSetup)
        {
            this.PrintLog("셋업이 정상적으로 진행되지 않았습니다.");
            return;
        }

        if (itemSO == null)
        {
            this.PrintLog("해당 아이템의 SO 데이터가 비어 있어서 인벤토리에 정상적인 저장을 할 수 없습니다.");
            return;
        }

        int nullIndex = -1;
        // 비어있는 인덱스 체크
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] == null)
            {
                nullIndex = i;
                break;
            }
        }

        // 비어 있는 칸이 없다면
        if (nullIndex < 0)
        {
            // 지연 저장 상태라면
            if (isDelaySave)
            {
                _delaySaveItemList.Add(itemSO);
                this.PrintLog(" 지연저장 가능 아이템을 지연 저장 파트에 넣어 처리합니다.");
            }
            else
                this.PrintLog($"인벤토리가 다 차있어 저장을 못합니다.[저장 시도 아이템 {itemSO.name}_{itemSO.itemID}]");
            return;
        }

        _items[nullIndex] = itemSO;
        this.PrintLog($"아이템을 저장합니다.[저장 인덱스: {nullIndex} / 저장 아이템 {itemSO.name}_{itemSO.itemID}]");
    }

    /// <summary>
    /// 보유 아이템, 인텍스로 제거
    /// </summary>
    /// <param name="index">제거 하고 싶은 보유 배열 인덱스</param>
    public void RemoveItemForIndex(int index)
    {
        if (!_isSetup)
        {
            this.PrintLog("셋업이 정상적으로 진행되지 않았습니다.");
            return;
        }

        if (_items.Length <= 0 || _items.Length <= index)
        {
            this.PrintLog($"인벤토리 저장 가능 수를 초과 하던가, 잘 못된 인텍스입니다. (Index => {index})");
            return;
        }

        UnimoItemSO resultItem = _items[index];
        RemoveItemForSO(resultItem);
    }

    /// <summary>
    /// 보유 아이템, 아이디로 제거
    /// </summary>
    /// <param name="id">제거 하고 싶은 아이템 ID</param>
    public void RemoveItemForID(int id)
    {
        if (!_isSetup)
        {
            this.PrintLog("셋업이 정상적으로 진행되지 않았습니다.");
            return;
        }

        if (id <= 0)
        {
            this.PrintLog($"음수의 아이템 아이디가 들어왔습니다.{id}");
            return;
        }

        UnimoItemSO resultItem = null;
        foreach (var item in _items)
        {
            if (id.Equals(item.itemID))
            {
                resultItem = item;
                break;
            }
        }

        RemoveItemForSO(resultItem);
    }

    public void RemoveItemForSO(UnimoItemSO inItemSO)
    {
        if (!_isSetup)
        {
            this.PrintLog("셋업이 정상적으로 진행되지 않았습니다.");
            return;
        }

        if (inItemSO == null)
        {
            this.PrintLog("제거 하고자하는 아이템 SO가 존재 하지않습니다.");
            return;
        }

        int removeIndex = -1;
        for (int i = 0; i < _items.Length; i++)
        {
            var item = _items[i];
            if (inItemSO.itemID.Equals(item.itemID))
            {
                removeIndex = i;
                break;
            }
        }

        if (removeIndex < 0)
        {
            this.PrintLog("제거할 수 있는 오브젝트가 존재 하지않습니다.");
            return;
        }

        var removeItem = _items[removeIndex];
        _items[removeIndex] = null;
        this.PrintLog($"인벤토리 제거 대상 아이템(=> {removeItem.itemName}) > 인벤토리에서 제거(=> 제거 여부: {_items[removeIndex] == null})");

        // 아이템 지연 저장 기능
        DelaySaveItem(removeIndex);
    }

    private void DelaySaveItem(int saveIndex)
    {
        if (_delaySaveItemList.Count > 0)
        {
            this.PrintLog($"지연 아이템 저장 작업을 진행(=> 지연 저장 가능 아이템 수: {_delaySaveItemList.Count})");
            UnimoItemSO delaySaveItem = null;
            // 데이터 유실 확인
            for (int i = 0; i < _delaySaveItemList.Count; i++)
            {
                if (_delaySaveItemList[i] != null)
                {
                    delaySaveItem = _delaySaveItemList[i];
                    break;
                }

                this.PrintLog($"지연 아이템 저장에 사용될 아이템이 유실 되었습니다. 제거 작업을 진행합니다.");
                _delaySaveItemList.RemoveAt(i);
                i--;
            }

            // 모든 데이터 유실 시, 처리
            if (_delaySaveItemList.Count <= 0)
            {
                this.PrintLog($"지연 아이템 저장에 사용될 아이템들 유실 되어, 지연 아이템 저장을 진행 할 수 없습니다.");
                return;
            }

            // 데이터 받고도 유실 시, 처리
            if (delaySaveItem == null)
            {
                this.PrintLog($"Delay Save Item 데이터가 유실 되어서 습니다. 기존에 진행하려고 했던 지연 아이템 저장 기능을 사용할 수 없습니다. > 필요 없는 데이터들을 정리합니다.");
                return;
            }

            _items[saveIndex] = delaySaveItem;
            this.PrintLog($"지연 아이템 저장 완료. 되었습니다.(=> 지연 저장 아이템 정보: {delaySaveItem.name}_{delaySaveItem.itemID})");
        }
    }


    public void PrintLog(string printLog, LogType type = LogType.Log)
    {
        this.PrintLog(printLog, type, Color.cyan);
    }
}