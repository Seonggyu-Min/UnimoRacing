using Firebase.Database;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class BuyButtonBehaviour : MonoBehaviour
    {

        public enum ItemType { Kart, Unimo }
        [SerializeField] private ItemType itemType;
        private int _itemId;

        [SerializeField] private TMP_Text _itemName;
        [SerializeField] private Image _itemIcon;
        [SerializeField] private Image _currencyImage;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_Text _buyButtonText;
        [SerializeField] private Button _buyButton;

        // 인스펙터에 화폐 스프라이트를 참조하도록 설정합니다.
        [SerializeField] private Sprite _gameMoneySprite;
        [SerializeField] private Sprite _cashSprite;

        private int _itemCost;
        private MoneyType _moneyType;

        public Button Button => _buyButton;
        private string CurrentUid => FirebaseManager.Instance?.Auth?.CurrentUser?.UserId;

        public void SetupButton(string name, Sprite itemSprite, string price, Sprite currencyType)
        {
            _itemName.text = name;
            _itemIcon.sprite = itemSprite;
            _priceText.text = price;
            _currencyImage.sprite = currencyType;
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
            // 1. Firebase에서 받은 currentLevel 값을 멤버 변수에 즉시 저장합니다.
            this._currentLevel = currentLevel;

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
            Debug.Log($"Item ID: {_itemId}, Cost: {_itemCost}, Money Type: {_moneyType}");
            Debug.Log($"_moneyType: {_moneyType}, GameMoneySprite null: {_gameMoneySprite == null}, CashSprite null: {_cashSprite == null}");
            
            // _currentLevel 값이 이제 올바르게 설정되었으므로,
            // 이 값을 사용하여 UI 상태를 결정합니다.
            if (_currentLevel > 0)
            {
                // 이미 소유한 아이템
                _buyButton.interactable = false;
                _buyButtonText.text = "보유 중";
                _priceText.text = ""; // 가격 텍스트 숨기기
                _currencyImage.enabled = false; // 화폐 이미지 숨기기

                if (_moneyType != MoneyType.None)
                {
                    _currencyImage.sprite = (_moneyType == MoneyType.Gold) ? _gameMoneySprite : _cashSprite;
                }
            }
            else
            {
                // 아직 소유하지 않은 아이템
                _buyButton.interactable = true;
                _buyButtonText.text = "구매하기";
                _priceText.text = _itemCost.ToString();
                _currencyImage.enabled = true; // 화폐 이미지 보이기

                // 화폐 아이콘 설정
                if (_moneyType != MoneyType.None)
                {
                    _currencyImage.sprite = (_moneyType == MoneyType.Gold) ? _gameMoneySprite : _cashSprite;
                }
            }

            // 예외 처리: 만약 아이템의 가격이 0이라면 구매 불가능
            if (_itemCost <= 0)
            {
                _buyButton.interactable = false;
                _priceText.text = "판매 불가";
            }
        }

        public void OnClickBuyButton()
        {
            // 이미 아이템을 소유하고 있다면 구매를 시도하지 않고 바로 종료
            if (_currentLevel > 0)
            {
                Debug.LogWarning("이미 소유한 아이템입니다.");
                _buyButton.interactable = false; // 혹시 모르니 비활성화 상태 유지
                return;
            }

            // 구매 시도를 시작할 때 버튼을 비활성화하여 중복 클릭을 막습니다.
            _buyButton.interactable = false;

            string uid = FirebaseManager.Instance?.Auth?.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogError("[ProcessPurchase] 사용자 UID가 유효하지 않습니다.");
                // 사용자 인증 오류 시 버튼을 다시 활성화
                _buyButton.interactable = true;
                return;
            }

            // 아이템 구매 트랜잭션 시작
            TrySpendTransaction(
                _moneyType,
                _itemCost,
                onDone =>
                {
                    // 트랜잭션이 완료되면 콜백이 호출됩니다.
                    if (!onDone) // 잔액 부족 등으로 구매 실패
                    {
                        Debug.LogError("잔액이 부족하거나, 불러오기에 실패 했습니다.");
                        // 구매 실패 시 버튼을 다시 활성화합니다.
                        _buyButton.interactable = true;
                        return;
                    }

                    // 구매 성공: 아이템을 인벤토리에 추가 (레벨 1로 설정)
                    string inventoryPath;
                    if (itemType == ItemType.Unimo)
                    {
                        inventoryPath = DBRoutes.UnimoInventory(uid, _itemId);
                    }
                    else
                    {
                        inventoryPath = DBRoutes.KartInventory(uid, _itemId);
                    }

                    DatabaseManager.Instance.SetOnMain(
                        inventoryPath,
                        1, // 획득이므로 무조건 레벨 1로 설정
                        () => Debug.Log("구매 완료"),
                        err => Debug.LogWarning($"{err} 구매 갱신 오류"));
                });
        }

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