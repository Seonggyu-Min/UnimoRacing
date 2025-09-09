using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    // SO에 Price가 사라짐으로써 사용되지 않습니다
    public class BuyButtonBehaviour : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _itemName;
        [SerializeField] private TMP_Text _itemPrice;

        private bool _amUnimoButton = false; // 버튼 자기 자신이 유니모를 파는지, 카트를 파는지 캐싱
        private UnimoSO _unimoSO; // 가격 확인을 위해 캐싱
        private KartSO _kartSO;
        private TMP_Text _infoText;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;


        public void SetupUnimo(UnimoSO unimoSO, TMP_Text infoText)
        {
            _unimoSO = unimoSO;
            _infoText = infoText;
            _amUnimoButton = true;
            _icon.sprite = unimoSO.Thumbnail;
            _itemName.text = unimoSO.Name;
            _itemPrice.text = $"{unimoSO.Price}";
        }

        public void SetupKart(KartSO kartSO, TMP_Text infoText)
        {
            _kartSO = kartSO;
            _infoText = infoText;
            _amUnimoButton = false;
            _icon.sprite = kartSO.Thumbnail;
            _itemName.text = kartSO.Name;
            _itemPrice.text = $"{kartSO.Price}";
        }

        public void SetOwnedVisual(bool owned)
        {
            _icon.color = owned ? Color.white : Color.gray; // 가지지 않은 것은 회색 처리
            _button.interactable = !owned; // 가진 것은 클릭 금지
        }
        private void Awake()
        {
            _button.onClick.AddListener(OnItemClicked);
        }

        // 아이템 상세 정보를 표시하는 메서드
        private void OnItemClicked()
        {
            if (_amUnimoButton)
            {
                _infoText.text = $"<color=yellow>{_unimoSO.Name}</color>\n\n{_unimoSO.Description}\n<color=yellow>가격: {_unimoSO.Price}</color>";
            }
            else
            {
                _infoText.text = $"<color=yellow>{_kartSO.Name}</color>\n\n{_kartSO.Description}\n<color=yellow>가격: {_kartSO.Price}</color>";
            }
        }

        public void OnClickBuyButton()
        {
            _button.interactable = false; // 중복 구매 방지

            MoneyType type;
            int price;

            if (_amUnimoButton)
            {
                type = _unimoSO.MoneyType;
                price = _unimoSO.Price;
            }
            else
            {
                type = _kartSO.MoneyType;
                price = _kartSO.Price;
            }

            TrySpendTransaction(type, price, ok =>
            {
                if (!ok)
                {
                    _infoText.text = "잔액이 부족하여 구매하지 못했습니다";
                    _button.interactable = true;
                    return;
                }

                _button.interactable = true;


                // TODO: 만약 여기서 실패하면 예외 처리 필요
                // 돈을 다시 돌려 놓아야 되나? -> 돈을 돌려 놓는 것도 실패할 수 있지 않나?

                if (_amUnimoButton)
                {
                    DatabaseManager.Instance.SetOnMain(DBRoutes.UnimoInventory(CurrentUid, _unimoSO.Index),
                        1,
                        () => _infoText.text = "구매 완료",
                        err => Debug.LogWarning($"인벤토리에 구매 갱신 중 오류: {err}")
                        );
                }
                else
                {
                    DatabaseManager.Instance.SetOnMain(DBRoutes.KartInventory(CurrentUid, _kartSO.Index),
                        1,
                        () => _infoText.text = "구매 완료",
                        err => Debug.LogWarning($"인벤토리에 구매 갱신 중 오류: {err}")
                        );
                }
            });
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
