using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG.Deprecated
{
    // SO에 Price가 사라짐으로써 사용되지 않습니다
    public class ShopUIBehaviour : MonoBehaviour
    {
        #region Fields and Properties

        [Header("Register SO")]
        [SerializeField] private UnimoCharacterSO[] _unimoSOs;
        [SerializeField] private UnimoKartSO[] _kartSOs;

        [Header("Shop Panel")]
        [SerializeField] private GameObject _unimoShopPanel;
        [SerializeField] private GameObject _kartShopPanel;

        [Header("Shop Panel Change Buttons")]
        [SerializeField] private Button _unimoShopButton;
        [SerializeField] private Button _kartShopButton;

        [Header("Button Parent")]
        [SerializeField] private Transform _unimoParent;
        [SerializeField] private Transform _kartParent;
        [SerializeField] private BuyButtonBehaviour _buyButtonPrefab;

        [Header("Info Text")]
        [SerializeField] private TMP_Text _infoText;

        // 버튼 인덱스로 조회용
        private Dictionary<int, BuyButtonBehaviour> _unimoDict = new();
        private Dictionary<int, BuyButtonBehaviour> _kartDict = new();

        // 패널 및 버튼 UI 상태
        private bool _showUnimoPanel = true;
        private bool _isGenerated = false;

        // 구독 해제 캐싱용 Action
        private Action _unsubUnimoInv;
        private Action _unsubKartInv;

        // 헬퍼 프로퍼티
        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            if (!_isGenerated)
            {
                GenerateButtons();
                _isGenerated = true;
            }

            ShowUnimoShop(_showUnimoPanel);
            SubscribeInventory();
        }

        private void OnDisable()
        {
            UnsubscribeInventory();
        }

        #endregion

        #region Button Methods

        public void OnClickUnimoShopButton() => ShowUnimoShop(true);
        public void OnClickKartShopButton() => ShowUnimoShop(false);

        #endregion

        #region Private Methods

        private void ShowUnimoShop(bool showUnimoPanel)
        {
            _showUnimoPanel = showUnimoPanel;
            _unimoShopPanel.SetActive(_showUnimoPanel);
            _kartShopPanel.SetActive(!_showUnimoPanel);
        }

        private void GenerateButtons()
        {
            // 유니모 버튼 생성
            for (int i = 0; i < _unimoSOs.Length; i++)
            {
                UnimoCharacterSO so = _unimoSOs[i];
                if (so == null) continue;

                BuyButtonBehaviour button = Instantiate(_buyButtonPrefab, _unimoParent);
                button.name = $"UnimoButton_{so.characterName}";

                button.SetupButton(so.characterName, so.characterSprite, string.Empty);
                button.SetupTypeAndId(BuyButtonBehaviour.ItemType.Unimo, so.characterId);

                _unimoDict.Add(so.characterId, button);
                button.RefreshItemState(0); // 초기 상태는 0으로 설정
            }

            // 카트 버튼 생성
            for (int i = 0; i < _kartSOs.Length; i++)
            {
                UnimoKartSO so = _kartSOs[i];
                if (so == null) continue;

                BuyButtonBehaviour button = Instantiate(_buyButtonPrefab, _kartParent);
                button.name = $"KartButton_{so.carName}";

                button.SetupButton(so.carName, so.kartSprite, string.Empty);
                button.SetupTypeAndId(BuyButtonBehaviour.ItemType.Kart, so.KartID);

                _kartDict.Add(so.KartID, button);
                button.RefreshItemState(0); // 초기 상태는 0으로 설정
            }
        }

        private void SubscribeInventory()
        {
            _unsubUnimoInv = DatabaseManager.Instance.SubscribeValueChanged(
                DBRoutes.UnimosInventory(CurrentUid),
                onChanged: OnUnimoInventorySnapshot,
                onError: (err) => Debug.LogWarning($"[ShopUIBehaviour] 유니모 인벤토리 구독 오류: {err}")
            );

            _unsubKartInv = DatabaseManager.Instance.SubscribeValueChanged(
                DBRoutes.KartsInventory(CurrentUid),
                onChanged: OnKartInventorySnapshot,
                onError: (err) => Debug.LogWarning($"[ShopUIBehaviour] 카트 인벤토리 구독 오류: {err}")
            );
        }

        private void UnsubscribeInventory()
        {
            _unsubUnimoInv?.Invoke();
            _unsubKartInv?.Invoke();
            _unsubUnimoInv = null;
            _unsubKartInv = null;
        }

        // 유니모 변화 콜백
        private void OnUnimoInventorySnapshot(DataSnapshot snap)
        {
            if (!snap.Exists || snap.Value == null)
            {
                foreach (var kv in _unimoDict) { kv.Value.RefreshItemState(0); }
                return;
            }

            Dictionary<string, object> inventoryData = snap.Value as Dictionary<string, object>;
            foreach (KeyValuePair<int, BuyButtonBehaviour> kv in _unimoDict)
            {
                int unimoId = kv.Key;
                int currentLevel = 0;
                if (inventoryData != null && inventoryData.ContainsKey(unimoId.ToString()))
                {
                    if (int.TryParse(inventoryData[unimoId.ToString()].ToString(), out int level))
                    {
                        currentLevel = level;
                    }
                }
                kv.Value.RefreshItemState(currentLevel);
            }
        }

        // 카트 변화 콜백
        private void OnKartInventorySnapshot(DataSnapshot snap)
        {
            if (!snap.Exists || snap.Value == null)
            {
                foreach (var kv in _kartDict) { kv.Value.RefreshItemState(0); }
                return;
            }

            Dictionary<string, object> inventoryData = snap.Value as Dictionary<string, object>;
            foreach (KeyValuePair<int, BuyButtonBehaviour> kv in _kartDict)
            {
                int kartId = kv.Key;
                int currentLevel = 0;
                if (inventoryData != null && inventoryData.ContainsKey(kartId.ToString()))
                {
                    if (int.TryParse(inventoryData[kartId.ToString()].ToString(), out int level))
                    {
                        currentLevel = level;
                    }
                }
                kv.Value.RefreshItemState(currentLevel);
            }
        }

        #endregion
    }
}