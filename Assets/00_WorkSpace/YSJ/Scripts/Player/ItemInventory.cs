using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;
using YSJ.Util;

public class ItemInventory : MonoBehaviour
{
    #region Parameters
    private const int DEFAULT_INVENTORY_NORMAL_SAVE_MAX_COUNT = 2;
    private const int DEFAULT_INVENTORY_DELAY_SAVE_MAX_COUNT = 1;

    [Header("Config")]
    [SerializeField] private bool _selfSetup = false;
    [SerializeField] private int _inventroyNormalSaveMaxCount = 2;
    [SerializeField] private int _inventroyDelaySaveMaxCount = 1;

    // Events
    public Action<UnimoItemSO> OnSaveItem;
    public Action<UnimoItemSO> OnRemoveItem;
    public Action OnChanged;

    // private
    private bool _isSetup = false;
    [SerializeField] private bool _isSavable = false;

    private PhotonView _ownerView;
    private PlayerRaceData _data;

    private UnimoItemSO[] _items;
    private readonly List<UnimoItemSO> _delaySaveItemList = new(); // 대기열(슬롯 꽉 찼을 때)

    // Public
    public bool IsSetup => _isSetup;
    public int Capacity => _items?.Length ?? 0;
    #endregion

    private void Awake()
    {
        if (_selfSetup)
            Setup(null);
    }

    public void Setup(PlayerRaceData data = null)
    {
        if (_isSetup)
        {
            this.PrintLog("이미 셋업되어 있어 무시됩니다.");
            return;
        }

        _ownerView = GetComponentInParent<PhotonView>();
        _data = data;
        if (_data == null)
            _data = GetComponent<PlayerRaceData>();

        int size = (_inventroyNormalSaveMaxCount > 0) ? _inventroyNormalSaveMaxCount : DEFAULT_INVENTORY_NORMAL_SAVE_MAX_COUNT;
        _items = new UnimoItemSO[size];

        _inventroyDelaySaveMaxCount = (_inventroyDelaySaveMaxCount > 0) ? _inventroyDelaySaveMaxCount : DEFAULT_INVENTORY_DELAY_SAVE_MAX_COUNT;

        // 셋업 완료 조건: 자기 셋업 플래그 or 외부 데이터 주입
        _isSetup = (_selfSetup || _data != null);
    }

    #region Save
    /// <summary>
    /// 인벤토리에 아이템 저장.
    /// - isDelaySave: 슬롯이 꽉 찼을 때 큐에 쌓기.
    /// - isForceSave: 저장 게이트( _isSavable )가 닫혀도 강제 저장.
    /// - isSaveItemAction: OnSaveItem 이벤트 트리거 여부.
    /// </summary>
    public void SaveItem(UnimoItemSO itemSO, bool isDelaySave = false, bool isForceSave = false, bool isSaveItemAction = true)
    {
        if (itemSO == null)
        {
            this.PrintLog("SaveItem 실패: itemSO == null");
            return;
        }

        if (!_isSetup)
        {
            this.PrintLog($"SaveItem 거부: 인벤토리 셋업 미완. 요청 SO: {itemSO.name}({itemSO.itemID})");
            return;
        }

        // 저장 게이트
        if (!_isSavable && !isForceSave)
        {
            this.PrintLog($"SaveItem 거부: 현재 저장 불가 상태(강제저장 아님). 요청 SO: {itemSO.name}({itemSO.itemID})");
            return;
        }

        // SO 정책 체크(원하면 강제저장으로 무시 가능)
        if (!itemSO.isInventorySavable && !isForceSave)
        {
            this.PrintLog($"SaveItem 거부: SO가 인벤토리 저장 비활성 상태. {itemSO.name}({itemSO.itemID})");
            return;
        }

        int empty = FindFirstEmptySlot();
        if (empty < 0)
        {
            // 슬롯 꽉 참
            if (isDelaySave)
            {
                if (_inventroyDelaySaveMaxCount < _delaySaveItemList.Count)
                {
                    _delaySaveItemList.Add(itemSO);
                    this.PrintLog($"슬롯 가득 > DelayQueue Enqueue: {itemSO.name}({itemSO.itemID}) / 대기:{_delaySaveItemList.Count}");
                }
                else
                {
                    this.PrintLog($"딜레이 슬롯 가득 > Slot Count : {_delaySaveItemList.Count} / {_inventroyDelaySaveMaxCount} << (현재/최대)");
                }
            }
            else
            {
                this.PrintLog($"SaveItem 실패: 슬롯 가득 & 지연저장 비활성. {itemSO.name}({itemSO.itemID})");
            }
            return;
        }

        _items[empty] = itemSO;

        if (isSaveItemAction) OnSaveItem?.Invoke(itemSO);
        OnChanged?.Invoke();

        this.PrintLog($"SaveItem 성공: idx={empty}, {itemSO.name}({itemSO.itemID})");
    }

    /// <summary>
    /// 특정 슬롯이 비면, DelayQueue에서 하나 꺼내 바로 채운다.
    /// </summary>
    private void TryFlushDelayQueueToSlot(int saveIndex)
    {
        if (_delaySaveItemList.Count <= 0) return;

        // 유실된 항목 정리 + 첫 유효 항목 선택
        UnimoItemSO picked = null;
        for (int i = 0; i < _delaySaveItemList.Count; i++)
        {
            if (_delaySaveItemList[i] == null)
            {
                _delaySaveItemList.RemoveAt(i);
                i--;
                continue;
            }
            picked = _delaySaveItemList[i];
            _delaySaveItemList.RemoveAt(i);
            break;
        }

        if (picked == null)
        {
            this.PrintLog("DelayQueue 비움: 유효 아이템 없음.");
            return;
        }

        // 슬롯이 여전히 비어있는지 최종 확인
        if (_items[saveIndex] == null)
        {
            _items[saveIndex] = picked;
            OnSaveItem?.Invoke(picked);
            OnChanged?.Invoke();
            this.PrintLog($"DelaySave 적용: idx={saveIndex}, {picked.name}({picked.itemID})");
        }
        else
        {
            // 레이스 컨디션으로 다른 경로에서 채워졌을 수 있음 > 다시 큐 뒤로
            _delaySaveItemList.Add(picked);
            this.PrintLog("DelaySave 취소: 슬롯이 채워짐. 다시 큐에 적재.");
        }
    }
    #endregion

    #region Remove
    /// <summary> 인덱스로 제거 </summary>
    public void RemoveItemForIndex(int index, bool delaySavable = true)
    {
        var target = FindItemByIndex(index);
        RemoveItemBySO(target, delaySavable);
    }

    /// <summary> ID로 제거 </summary>
    public void RemoveItemByID(ItemId id, bool delaySavable = true)
    {
        var target = FindItemByID((int)id);
        RemoveItemBySO(target, delaySavable);
    }

    /// <summary> SO로 제거 </summary>
    public void RemoveItemBySO(UnimoItemSO inItemSO, bool delaySavable = true)
    {
        if (!_isSetup)
        {
            this.PrintLog("RemoveItem 거부: 셋업 미완");
            return;
        }

        if (inItemSO == null)
        {
            this.PrintLog("RemoveItem 거부: SO == null");
            return;
        }

        int removeIndex = -1;
        for (int i = 0; i < _items.Length; i++)
        {
            var item = _items[i];
            if (item == null) continue;
            if (item.itemID == inItemSO.itemID)
            {
                removeIndex = i;
                break;
            }
        }

        if (removeIndex < 0)
        {
            this.PrintLog("RemoveItem 실패: 대상 없음");
            return;
        }

        var removed = _items[removeIndex];
        _items[removeIndex] = null;

        OnRemoveItem?.Invoke(removed);
        OnChanged?.Invoke();

        this.PrintLog($"RemoveItem 성공: idx={removeIndex}, {removed.itemName}({removed.itemID})");

        // 지연 저장 사용 시, 빈 칸을 DelayQueue로 보충
        if (delaySavable) TryFlushDelayQueueToSlot(removeIndex);
    }
    #endregion

    #region Find
    public UnimoItemSO FindItemByIndex(int index)
    {
        if (!_isSetup)
        {
            this.PrintLog("FindItemByIndex 거부: 셋업 미완");
            return null;
        }

        if (_items == null || index < 0 || index >= _items.Length)
        {
            this.PrintLog($"FindItemByIndex 실패: 잘못된 인덱스 {index}");
            return null;
        }

        return _items[index];
    }

    public UnimoItemSO FindItemByID(int id)
    {
        if (!_isSetup)
        {
            this.PrintLog("FindItemByID 거부: 셋업 미완");
            return null;
        }

        if (id <= 0)
        {
            this.PrintLog($"FindItemByID 실패: 유효하지 않은 id={id}");
            return null;
        }

        foreach (var item in _items)
        {
            if (item == null) continue;
            if ((int)item.itemID == id) return item;
        }
        return null;
    }

    public int FindItemIndexBySO(UnimoItemSO itemSO)
    {
        if (itemSO == null)
        {
            this.PrintLog("FindItemIndexBySO 실패: SO == null");
            return -1;
        }

        for (int i = 0; i < _items.Length; i++)
        {
            var item = _items[i];
            if (item == null) continue;
            if (item.itemID == itemSO.itemID) return i;
        }
        return -1;
    }

    public bool ItemContains(UnimoItemSO itemSO)
    {
        int idx = FindItemIndexBySO(itemSO);
        return (idx >= 0);
    }

    public int ItemCount()
    {
        if (_items == null) return 0;
        int cnt = 0;
        for (int i = 0; i < _items.Length; i++)
            if (_items[i] != null) cnt++;
        return cnt;
    }

    private int FindFirstEmptySlot()
    {
        if (_items == null) return -1;
        for (int i = 0; i < _items.Length; i++)
            if (_items[i] == null) return i;
        return -1;
    }
    #endregion

    #region Util 
    /// <summary> 외부에서 저장 가능 토글  </summary>
    public void SetSavable(bool isSavable)
    {
        _isSavable = isSavable;
        this.PrintLog($"SetSavable: {isSavable}");
    }

    /// <summary> 가장 앞(낮은 인덱스) 아이템을 소비 시도. 소비에 성공하면 제거. </summary>
    public bool TryConsumeFirst(Func<UnimoItemSO, bool> consumer, bool delaySavable = true)
    {
        if (consumer == null) return false;

        for (int i = 0; i < _items.Length; i++)
        {
            var item = _items[i];
            if (item == null) continue;

            bool ok = false;
            try { ok = consumer(item); }
            catch (Exception e)
            {
                this.PrintLog($"TryConsumeFirst 예외: {e.Message}", LogType.Error);
            }

            if (ok)
            {
                RemoveItemForIndex(i, delaySavable);
                return true;
            }
        }
        return false;
    }

    /// <summary> 인벤토리 초기화(슬롯 비우고 큐도 비움) </summary>
    public void ClearAll(bool invokeRemoveEventPerItem = false)
    {
        if (_items != null)
        {
            if (invokeRemoveEventPerItem)
            {
                for (int i = 0; i < _items.Length; i++)
                {
                    if (_items[i] != null) OnRemoveItem?.Invoke(_items[i]);
                    _items[i] = null;
                }
            }
            else
            {
                Array.Clear(_items, 0, _items.Length);
            }
        }

        _delaySaveItemList.Clear();
        OnChanged?.Invoke();
        this.PrintLog("인벤토리 초기화 완료");
    }

    /// <summary> 용량 변경(주의: 기존 아이템은 가능한 한 유지시키고 잘리면 뒤에서부터 삭제) </summary>
    public void Resize(int newSize)
    {
        if (newSize <= 0)
        {
            this.PrintLog("Resize 거부: newSize <= 0");
            return;
        }

        if (_items == null)
        {
            _items = new UnimoItemSO[newSize];
            _inventroyNormalSaveMaxCount = newSize;
            OnChanged?.Invoke();
            return;
        }

        if (newSize == _items.Length) return;

        var newArr = new UnimoItemSO[newSize];
        int copy = Mathf.Min(newSize, _items.Length);
        Array.Copy(_items, newArr, copy);

        if (newSize < _items.Length)
        {
            for (int i = newSize; i < _items.Length; i++)
            {
                if (_items[i] != null) OnRemoveItem?.Invoke(_items[i]);
            }
        }

        _items = newArr;
        _inventroyNormalSaveMaxCount = newSize;
        OnChanged?.Invoke();
        this.PrintLog($"Resize 완료: {newSize}");
    }
    #endregion

    #region Logging
    public void PrintLog(string printLog, LogType type = LogType.Log)
    {
        this.PrintLog(printLog, type, Color.cyan);
    }
    #endregion
}
