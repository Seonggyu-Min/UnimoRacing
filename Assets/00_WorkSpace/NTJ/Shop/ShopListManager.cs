using Firebase.Database;
using MSG;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopListManager : PopupBase
{
    #region Fields and Properties
    [Header("Register SO")]
    [SerializeField] private UnimoCharacterSO[] _unimoSOs;
    [SerializeField] private UnimoKartSO[] _kartSOs;

    [Header("Button Parent")]
    [SerializeField] private Transform _unimoParent;
    [SerializeField] private Transform _kartParent;

    [Header("Button Prefabs")]
    [SerializeField] private BuyButtonBehaviour _unimoButtonPrefab; // 유니모용 버튼 프리팹
    [SerializeField] private BuyButtonBehaviour _kartButtonPrefab; // 카트용 버튼 프리팹

    [Header("Info Text")]
    [SerializeField] private TMP_Text _infoText;

    private readonly Dictionary<int, BuyButtonBehaviour> _unimoDict = new();
    private readonly Dictionary<int, BuyButtonBehaviour> _kartDict = new();
    private bool _isGenerated = false;

    private Action _unsubUnimoInv;
    private Action _unsubKartInv;

    private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;
    #endregion

    #region Unity Methods
    private void OnEnable()
    {
        // OnEnable()은 구독만 시작합니다.
        // 버튼 생성은 데이터가 도착하면 스냅샷 콜백에서 처리합니다.
        SubscribeInventory();
    }

    private void OnDisable()
    {
        UnsubscribeInventory();
    }

    #endregion

    #region Private Methods

    private void GenerateButtons()
    {
        // 유니모 버튼 생성
        for (int i = 0; i < _unimoSOs.Length; i++)
        {
            UnimoCharacterSO so = _unimoSOs[i];
            if (so == null) continue;

            // Unimo 프리팹 사용
            BuyButtonBehaviour button = Instantiate(_unimoButtonPrefab, _unimoParent);
            button.name = $"UnimoButton_{so.characterName}";          
            button.SetupButton(so.characterName, so.characterSprite, string.Empty, so.currencyType);
            button.SetupTypeAndId(BuyButtonBehaviour.ItemType.Unimo, so.characterId);
            _unimoDict.Add(so.characterId, button);
            button.RefreshItemState(0);
        }

        // 카트 버튼 생성
        for (int i = 0; i < _kartSOs.Length; i++)
        {
            UnimoKartSO so = _kartSOs[i];
            if (so == null) continue;

            BuyButtonBehaviour button = Instantiate(_kartButtonPrefab, _kartParent);
            button.name = $"KartButton_{so.carName}";
            button.SetupButton(so.carName, so.kartSprite, string.Empty, so.currencyType);
            button.SetupTypeAndId(BuyButtonBehaviour.ItemType.Kart, so.KartID);
            _kartDict.Add(so.KartID, button);
            button.RefreshItemState(0);
        }
    }

    private void SubscribeInventory()
    {
        _unsubUnimoInv = DatabaseManager.Instance.SubscribeValueChanged(
            DBRoutes.UnimosInventory(CurrentUid),
            onChanged: OnUnimoInventorySnapshot,
            onError: (err) => Debug.LogWarning($"[ShopManager] 유니모 인벤토리 구독 오류: {err}")
        );

        _unsubKartInv = DatabaseManager.Instance.SubscribeValueChanged(
            DBRoutes.KartsInventory(CurrentUid),
            onChanged: OnKartInventorySnapshot,
            onError: (err) => Debug.LogWarning($"[ShopManager] 카트 인벤토리 구독 오류: {err}")
        );
    }

    private void UnsubscribeInventory()
    {
        _unsubUnimoInv?.Invoke();
        _unsubKartInv?.Invoke();
        _unsubUnimoInv = null;
        _unsubKartInv = null;
    }

    private void OnUnimoInventorySnapshot(DataSnapshot snap)
    {
        // 버튼이 아직 생성되지 않았다면, 지금 생성합니다.
        // 이렇게 하면 인벤토리 데이터를 가지고 있는 상태에서 UI를 만들 수 있습니다.
        if (!_isGenerated)
        {
            GenerateButtons();
            _isGenerated = true;
        }

        var inventoryData = snap.Value as Dictionary<string, object> ?? new Dictionary<string, object>();

        foreach (var kv in _unimoDict)
        {
            int unimoId = kv.Key;
            BuyButtonBehaviour button = kv.Value;
            if (inventoryData.ContainsKey(unimoId.ToString()))
            {
                button.RefreshItemState(1); // 1: 소유 중
            }
            else
            {
                button.RefreshItemState(0); // 0: 소유하지 않음
            }
        }
    }

    private void OnKartInventorySnapshot(DataSnapshot snap)
    {
        // 버튼이 아직 생성되지 않았다면, 지금 생성합니다.
        if (!_isGenerated)
        {
            GenerateButtons();
            _isGenerated = true;
        }

        var inventoryData = snap.Value as Dictionary<string, object> ?? new Dictionary<string, object>();
        foreach (var kv in _kartDict)
        {
            int kartId = kv.Key;
            BuyButtonBehaviour button = kv.Value;

            if (inventoryData.ContainsKey(kartId.ToString()))
            {
                // 카트의 경우 Firebase에서 실제 레벨을 가져옵니다.
                int currentLevel = 0;
                if (int.TryParse(inventoryData[kartId.ToString()].ToString(), out int level))
                {
                    currentLevel = level;
                }
                button.RefreshItemState(currentLevel);
            }
            else
            {
                button.RefreshItemState(0);
            }
        }
    }
    #endregion
}