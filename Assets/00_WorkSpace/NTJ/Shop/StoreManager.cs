using MSG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StoreManager : MonoBehaviour
{
    [SerializeField] private StoreItemsSO storeItems;

    public class ItemData
    {
        public int itemId;
        public string name;
        public Sprite icon;
        public BuyButtonBehaviour.ItemType itemType;
    }

    private List<ItemData> allItems;
    private Dictionary<int, ItemState> itemStatus = new Dictionary<int, ItemState>();

    private string CurrentUid => FirebaseManager.Instance?.Auth?.CurrentUser?.UserId;
    private int _completedChecks = 0;
    private int _totalItems = 0;

    // 아이템의 상태를 저장하는 클래스
    public class ItemState
    {
        public int itemId;
        public int currentLevel; // 0이면 미보유, 1 이상이면 보유
    }

    private void Start()
    {
        InitializeStore();
    }

    public void InitializeStore()
    {
        if (storeItems == null)
        {
            Debug.LogError("StoreItemsSO 스크립터블 오브젝트가 할당되지 않았습니다!");
            return;
        }

        allItems = LoadAllItems();
        _totalItems = allItems.Count;
        _completedChecks = 0;

        // 각 아이템의 보유 상태를 비동기적으로 확인하는 코루틴 시작
        StartCoroutine(CheckAllItemOwnership());
    }

    private List<ItemData> LoadAllItems()
    {
        var items = new List<ItemData>();

        // Kart SO 데이터를 ItemData로 변환
        foreach (var kartSO in storeItems.karts)
        {
            items.Add(new ItemData
            {
                itemId = kartSO.KartID,
                name = kartSO.carName,
                icon = kartSO.kartSprite,
                itemType = BuyButtonBehaviour.ItemType.Kart
            });
        }

        // Unimo SO 데이터를 ItemData로 변환
        foreach (var unimoSO in storeItems.unimos)
        {
            items.Add(new ItemData
            {
                itemId = unimoSO.characterId,
                name = unimoSO.characterName,
                icon = unimoSO.characterSprite,
                itemType = BuyButtonBehaviour.ItemType.Unimo
            });
        }

        return items;
    }

    private IEnumerator CheckAllItemOwnership()
    {
        if (string.IsNullOrEmpty(CurrentUid))
        {
            Debug.LogError("사용자 UID를 찾을 수 없습니다.");
            yield break;
        }

        foreach (var item in allItems)
        {
            string inventoryPath = GetInventoryPath(item.itemId, item.itemType);

            DatabaseManager.Instance.GetOnMain(
                inventoryPath,
                snap =>
                {
                    int currentLevel = 0;
                    if (snap.Exists && snap.Value != null)
                    {
                        int.TryParse(snap.Value.ToString(), out currentLevel);
                    }

                    if (!itemStatus.ContainsKey(item.itemId))
                    {
                        itemStatus.Add(item.itemId, new ItemState { itemId = item.itemId, currentLevel = currentLevel });
                    }
                    else
                    {
                        itemStatus[item.itemId].currentLevel = currentLevel;
                    }
                    _completedChecks++;
                },
                err =>
                {
                    Debug.LogError($"인벤토리 조회 실패: {err}");
                    _completedChecks++;
                }
            );
        }

        // 모든 비동기 호출이 완료될 때까지 대기
        while (_completedChecks < _totalItems)
        {
            yield return null;
        }

        // 모든 상태 확인 후 정렬 및 표시
        SortAndDisplayItems();
    }

    private string GetInventoryPath(int itemId, BuyButtonBehaviour.ItemType itemType)
    {
        return itemType == BuyButtonBehaviour.ItemType.Kart
            ? DBRoutes.KartInventory(CurrentUid, itemId)
            : DBRoutes.UnimoInventory(CurrentUid, itemId);
    }

    public void SortAndDisplayItems()
    {
        // 1. 보유 여부(보유 중이면 아래로)를 기준으로 1차 정렬
        // 2. 오브젝트 ID(itemId)를 기준으로 2차 정렬 (오름차순)
        List<ItemData> sortedList = allItems.OrderBy(item =>
        {
            if (itemStatus.TryGetValue(item.itemId, out var status))
            {
                return status.currentLevel > 0 ? 1 : 0;
            }
            return 0;
        })
        .ThenBy(item => item.itemId)
        .ToList();

        // 정렬된 목록을 UI에 표시하는 메서드 호출
        DisplayItemsInUI(sortedList);
    }

    private void DisplayItemsInUI(List<ItemData> itemsToDisplay)
    {
        // TODO: 이 부분에서 UI GameObject들을 순서대로 재활용하거나 새로 생성하여 표시하는 로직을 구현합니다.
        // 예시: 상점 UI 패널의 자식 오브젝트로 BuyButtonBehaviour 프리팹들을 생성하고,
        // sortedList의 데이터를 사용하여 각 프리팹의 정보를 설정합니다.

        Debug.Log("정렬된 아이템 목록:");
        foreach (var item in itemsToDisplay)
        {
            string status = itemStatus.ContainsKey(item.itemId) && itemStatus[item.itemId].currentLevel > 0
                ? "보유 중"
                : "미보유";

            Debug.Log($"ID: {item.itemId}, 이름: {item.name}, 상태: {status}");
        }
    }
}
