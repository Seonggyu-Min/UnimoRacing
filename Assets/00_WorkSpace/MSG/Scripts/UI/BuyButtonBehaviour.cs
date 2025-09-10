using Firebase.Database;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    // SO에 Price가 사라짐으로써 사용되지 않습니다
    public class BuyButtonBehaviour : MonoBehaviour
    {

        public enum ItemType { Kart, Unimo }
        [SerializeField] private ItemType itemType;
        private int _itemId; // private 필드로 선언

        [SerializeField] private TMP_Text _itemName;
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_Text _buyButtonText;
        [SerializeField] private Button _buyButton;

        private int _itemCost;
        private MoneyType _moneyType;

        public Button Button => _buyButton;
        private string CurrentUid => FirebaseManager.Instance?.Auth?.CurrentUser?.UserId;

        public void SetupButton(string name, Sprite sprite, string priceString)
        {
            _itemName.text = name;
            _itemIcon.sprite = sprite;
            _priceText.text = priceString;
        }

        private void Start()
        {
            if (_buyButton != null)
            {
                _buyButton.onClick.AddListener(OnClickBuyButton);
            }
        }

        private int _currentLevel = 0;
        private bool _amUnimoButton;

        public void SetupTypeAndId(ItemType type, int id)
        {
            this.itemType = type;
            this._itemId = id; // _itemId에 할당
        }

        public void RefreshItemState(int currentLevel)
        {
            if (itemType == ItemType.Kart)
            {
                PatchService.Instance.GetCostOfKart(
                    _itemId,
                    (cost, moneyType) =>
                    {
                        _itemCost = cost;
                        _moneyType = moneyType;
                        UpdateUI();
                    },
                    err => Debug.LogError($"가격 불러오기 실패: {err}")
                );
            }
            else // ItemType.Unimo
            {
                PatchService.Instance.GetCostOfUnimo(
                    _itemId,
                    (cost, moneyType) =>
                    {
                        _itemCost = cost;
                        _moneyType = moneyType;
                        UpdateUI();
                    },
                    err => Debug.LogError($"가격 불러오기 실패: {err}")
                );
            }
        }

        private void UpdateUI()
        {
            if (itemType == ItemType.Kart)
            {
                _buyButtonText.text = _currentLevel > 0 ? "강화" : "획득";
            }
            else
            {
                _buyButtonText.text = _currentLevel > 0 ? "보유 중" : "획득";
            }

            if (_itemCost <= 0)
            {
                _buyButton.interactable = false;
                _priceText.text = (itemType == ItemType.Kart) ? "최대 레벨" : "보유 중";
            }
            else
            {
                _buyButton.interactable = true;
                string moneySuffix = _moneyType == MoneyType.BlueHoneyGem ? "C" : "G";
                _priceText.text = $"{_itemCost} {moneySuffix}";
            }
        }

        public void ProcessPurchase(int itemId, BuyButtonBehaviour.ItemType itemType, int cost, MoneyType moneyType)
        {

        }

        public void OnClickBuyButton()
        {
            string uid = FirebaseManager.Instance?.Auth?.CurrentUser?.UserId;

            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogError("[ProcessPurchase] 사용자 UID가 유효하지 않습니다.");
                return;
            }

            string moneyPath = GetMoneyPath(_moneyType, uid);

            TrySpendTransaction(_moneyType,
                _itemCost,
                onDone =>
                {
                    if (!onDone)
                    {
                        Debug.LogError("잔액이 부족하거나, 불러오기에 실패 했습니다.");
                        return;
                    }
                    if (itemType == ItemType.Unimo)
                    {
                        DatabaseManager.Instance.SetOnMain(DBRoutes.UnimoInventory(uid, _itemId),
                            1,
                            () => Debug.Log("구매 완료"),
                        err => Debug.LogWarning($"{err} 구매 갱신 오류"));
                    }
                    else
                    {
                        DatabaseManager.Instance.SetOnMain(DBRoutes.KartInventory(uid, _itemId),
                                        1,
                                        () => Debug.Log("구매 완료"),
                            err => Debug.LogWarning($"{err} 구매 갱신 오류"));
                    }
                });
        }

    // DatabaseManager.Instance.IncrementToLongOnMainWithTransaction(
    //     moneyPath,
    //     -_itemCost,
    //     (long newBalance) =>
    //     {
    //         // 트랜잭션이 성공했습니다! 아이템을 지급합니다.
    //         // 성공 콜백이 호출되었다는 것은 잔액이 충분했다는 의미입니다.
    //         SetItemInventory(_itemId, itemType, uid);
    //         Debug.Log($"구매 성공! 새로운 잔액: {newBalance}");
    //     },
    //     (error) =>
    //     {
    //         // 트랜잭션이 실패했습니다. 여기서 잔액 부족을 처리합니다.
    //         // Firebase의 권한 거부(permission_denied) 에러가 잔액 부족을 의미할 수 있습니다.
    //         if (error.Contains("permission_denied"))
    //         {
    //             Debug.LogWarning("[ProcessPurchase] 잔액이 부족하여 구매에 실패했습니다.");
    //         }
    //         else
    //         {
    //             Debug.LogError($"[ProcessPurchase] 코인 거래 실패: {error}");
    //         }
    //     }
    // );


// 아이템 인벤토리를 업데이트하는 헬퍼 메서드
private void SetItemInventory(int itemId, BuyButtonBehaviour.ItemType itemType, string uid)
{
    string inventoryPath;
    int level = 1; // 기본적으로 아이템을 획득하면 레벨 1로 설정

    if (itemType == BuyButtonBehaviour.ItemType.Kart)
    {
        inventoryPath = DBRoutes.KartInventory(uid, itemId);
    }
    else // Unimo
    {
        inventoryPath = DBRoutes.UnimoInventory(uid, itemId);
    }

    DatabaseManager.Instance.SetOnMain(inventoryPath, level,
        () => Debug.Log($"아이템 {itemId} 획득/강화 성공!"),
        err => Debug.LogError($"아이템 {itemId} 획득 실패: {err}")
    );
}


private string GetMoneyPath(MoneyType moneyType, string uid)
{
    return moneyType switch
    {
        MoneyType.Gold => DBRoutes.Gold(uid),
        MoneyType.BlueHoneyGem => DBRoutes.BlueHoneyGem(uid),
        _ => throw new ArgumentException("유효하지 않은 코인 타입입니다.")
    };
}


// 분기가 여러개라서 메서드 분리
private void TrySpendTransaction(MoneyType moneyType, int price, Action<bool> onDone)
{
    string path = moneyType switch
    {
        MoneyType.Gold => DBRoutes.Gold(CurrentUid),
        MoneyType.BlueHoneyGem => DBRoutes.BlueHoneyGem(CurrentUid),
        //MoneyType.Money3 => DBRoutes.Money3(CurrentUid),
        _ => null
    };

    if (path == null)
    {
        onDone?.Invoke(false);
        return;
    }

    DatabaseManager.Instance.RunTransactionOnMain(
        path,
        mutable =>
        {
            long current = 0;
            try
            {
                if (mutable.Value != null)
                {
                    current = Convert.ToInt64(mutable.Value);
                }
            }
            catch
            {
                // 파싱 실패는 0으로 간주
            }

            // 부족하면 Abort
            if (current < price)
            {
                // 구매할 수 없다
                return TransactionResult.Abort();
            }

            // 충분하면 차감하고 Success
            mutable.Value = current - price;
            return TransactionResult.Success(mutable);
        },
        _ => onDone?.Invoke(true),
        _ => onDone?.Invoke(false)
    );
}
    }
}