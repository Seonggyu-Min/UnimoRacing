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

    // �������� ���¸� �����ϴ� Ŭ����
    public class ItemState
    {
        public int itemId;
        public int currentLevel; // 0�̸� �̺���, 1 �̻��̸� ����
    }

    private void Start()
    {
        InitializeStore();
    }

    public void InitializeStore()
    {
        if (storeItems == null)
        {
            Debug.LogError("StoreItemsSO ��ũ���ͺ� ������Ʈ�� �Ҵ���� �ʾҽ��ϴ�!");
            return;
        }

        allItems = LoadAllItems();
        _totalItems = allItems.Count;
        _completedChecks = 0;

        // �� �������� ���� ���¸� �񵿱������� Ȯ���ϴ� �ڷ�ƾ ����
        StartCoroutine(CheckAllItemOwnership());
    }

    private List<ItemData> LoadAllItems()
    {
        var items = new List<ItemData>();

        // Kart SO �����͸� ItemData�� ��ȯ
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

        // Unimo SO �����͸� ItemData�� ��ȯ
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
            Debug.LogError("����� UID�� ã�� �� �����ϴ�.");
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
                    Debug.LogError($"�κ��丮 ��ȸ ����: {err}");
                    _completedChecks++;
                }
            );
        }

        // ��� �񵿱� ȣ���� �Ϸ�� ������ ���
        while (_completedChecks < _totalItems)
        {
            yield return null;
        }

        // ��� ���� Ȯ�� �� ���� �� ǥ��
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
        // 1. ���� ����(���� ���̸� �Ʒ���)�� �������� 1�� ����
        // 2. ������Ʈ ID(itemId)�� �������� 2�� ���� (��������)
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

        // ���ĵ� ����� UI�� ǥ���ϴ� �޼��� ȣ��
        DisplayItemsInUI(sortedList);
    }

    private void DisplayItemsInUI(List<ItemData> itemsToDisplay)
    {
        // TODO: �� �κп��� UI GameObject���� ������� ��Ȱ���ϰų� ���� �����Ͽ� ǥ���ϴ� ������ �����մϴ�.
        // ����: ���� UI �г��� �ڽ� ������Ʈ�� BuyButtonBehaviour �����յ��� �����ϰ�,
        // sortedList�� �����͸� ����Ͽ� �� �������� ������ �����մϴ�.

        Debug.Log("���ĵ� ������ ���:");
        foreach (var item in itemsToDisplay)
        {
            string status = itemStatus.ContainsKey(item.itemId) && itemStatus[item.itemId].currentLevel > 0
                ? "���� ��"
                : "�̺���";

            Debug.Log($"ID: {item.itemId}, �̸�: {item.name}, ����: {status}");
        }
    }
}
