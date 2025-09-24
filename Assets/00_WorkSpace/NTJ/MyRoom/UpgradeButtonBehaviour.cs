using Firebase.Database;
using MSG;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeButtonBehaviour : MonoBehaviour
{
    [SerializeField] private int _itemId;

    [Header("UI References")]
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private Image _currencyImage;
    [SerializeField] private TMP_Text _upgradeButtonText;
    [SerializeField] private TMP_Text _currentSpeedText;
    [SerializeField] private TMP_Text _upgradeSpeedText;

    // 인스펙터에 화폐 스프라이트를 참조하도록 설정합니다.
    [SerializeField] private Sprite _gameMoneySprite;
    [SerializeField] private Sprite _cashSprite;

    private int _itemCost;
    private MSG.MoneyType _moneyType;
    private int _currentLevel;

    private string CurrentUid => FirebaseManager.Instance?.Auth?.CurrentUser?.UserId;

    private void Start()
    {
        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.AddListener(OnClickUpgradeButton);
        }

       // RefreshUpgradeState();
    }

    public void InitializeButton(int itemId)
    {
        _itemId = itemId;
        RefreshUpgradeState(); // 아이템 ID가 설정된 후에 UI 갱신 시작
    }

    /// <summary>
    /// 카트의 현재 상태(레벨, 스탯, 비용)를 조회하여 UI를 갱신합니다.
    /// </summary>
    public void RefreshUpgradeState()
    {
        string uid = CurrentUid;
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("[RefreshUpgradeState] 사용자 UID가 유효하지 않습니다.");
            UpdateUIForUnownedKart();
            return;
        }

        DatabaseManager.Instance.GetOnMain(
            DBRoutes.KartInventory(uid, _itemId),
            snap =>
            {
                _currentLevel = 0;
                if (snap.Exists && snap.Value != null)
                {
                    int.TryParse(snap.Value.ToString(), out _currentLevel);
                }

                if (_currentLevel <= 0)
                {
                    UpdateUIForUnownedKart();
                }
                else
                {
                    UpdateUIForOwnedKart();
                }
            },
            err =>
            {
                Debug.LogError($"인벤토리 조회 실패: {err}");
                UpdateUIForUnownedKart();
            }
        );
    }

    private void UpdateUIForUnownedKart()
    {
        _upgradeButton.interactable = false;
        _priceText.text = "미보유";
        _currencyImage.enabled = false;
        _upgradeButtonText.text = "구매 필요";
        _currentSpeedText.text = "";
        _upgradeSpeedText.text = "";
    }

    private void UpdateUIForOwnedKart()
    {
        PatchService.Instance.GetCostOfKart(
            _itemId,
            (cost, moneyType) =>
            {
                _itemCost = cost;
                _moneyType = moneyType;

                PatchService.Instance.GetSpeedOfKartUpgradeText(
                    _itemId,
                    (currentSpeed, nextSpeed) =>
                    {
                        bool isMaxLevel = (currentSpeed >= nextSpeed);
                        _currentSpeedText.text = $"부스터 속도 *{currentSpeed:F1}";

                        if (isMaxLevel)
                        {
                            _upgradeButton.interactable = false;
                            _priceText.text = "최대 레벨";
                            _currencyImage.enabled = false;
                            _upgradeButtonText.text = "최대 레벨";
                            _upgradeSpeedText.text = "";
                        }
                        else
                        {
                            _upgradeButton.interactable = true;
                            _priceText.text = _itemCost.ToString();
                            _currencyImage.enabled = true;
                            _upgradeButtonText.text = $"LV.{_currentLevel + 1} 강화하기";
                            _upgradeSpeedText.text = $"-> *{nextSpeed:F1}";
                            _currencyImage.sprite = (_moneyType == MSG.MoneyType.Gold) ? _gameMoneySprite : _cashSprite;
                        }
                    },
                    err => Debug.LogError($"속도 스탯 불러오기 실패: {err}")
                );
            },
            err => Debug.LogError($"강화 비용 불러오기 실패: {err}")
        );
    }

    private void OnClickUpgradeButton()
    {
        _upgradeButton.interactable = false;

        string uid = CurrentUid;
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("[ProcessUpgrade] 사용자 UID가 유효하지 않습니다.");
            _upgradeButton.interactable = true;
            return;
        }

        DatabaseManager.Instance.GetOnMain(
            DBRoutes.KartInventory(uid, _itemId),
            snap =>
            {
                int currentLevel = 0;
                if (snap.Exists && snap.Value != null)
                {
                    int.TryParse(snap.Value.ToString(), out currentLevel);
                }

                if (currentLevel <= 0)
                {
                    Debug.LogError("강화하려는 카트가 인벤토리에 없습니다.");
                    RefreshUpgradeState();
                    return;
                }

                TrySpendTransaction(
                    _moneyType,
                    _itemCost,
                    onDone =>
                    {
                        if (!onDone)
                        {
                            Debug.LogError("잔액이 부족하거나 트랜잭션 실패");
                            RefreshUpgradeState();
                            return;
                        }

                        string inventoryPath = DBRoutes.KartInventory(uid, _itemId);
                        int newLevel = currentLevel + 1;

                        DatabaseManager.Instance.SetOnMain(
                            inventoryPath,
                            newLevel,
                            () =>
                            {
                                Debug.Log($"카트 {_itemId} 강화 성공! 새 레벨: {newLevel}");
                                RefreshUpgradeState();
                            },
                            err =>
                            {
                                Debug.LogError($"레벨 업데이트 실패: {err}");
                                RefreshUpgradeState();
                            }
                        );
                    });
            },
            err =>
            {
                Debug.LogError($"인벤토리 조회 실패: {err}");
                RefreshUpgradeState();
            }
        );
    }

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
                    Debug.LogError("트랜잭션: 데이터 파싱 실패. Aborting.");
                    return TransactionResult.Abort();
                }

                // 잔액 부족 확인은 한 번만 수행합니다.
                if (current < price)
                {
                    Debug.LogWarning("트랜잭션: 잔액 부족. Aborting.");
                    return TransactionResult.Abort();
                }

                mutable.Value = current - price;
                return TransactionResult.Success(mutable);
            },
            // onSuccess, onError 콜백은 기존과 동일합니다.
            snap => onDone?.Invoke(true),
            errMsg => {
                Debug.LogError($"트랜잭션 실패: {errMsg}");
                onDone?.Invoke(false);
            }
        );
    }
}