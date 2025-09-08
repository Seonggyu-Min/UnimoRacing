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
        [SerializeField] private UnimoSO[] _unimoSOs;
        [SerializeField] private KartSO[] _kartSOs;

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

            if (_showUnimoPanel)
            {
                _unimoShopPanel.SetActive(true);
                _kartShopPanel.SetActive(false);
            }
            else
            {
                _unimoShopPanel.SetActive(false);
                _kartShopPanel.SetActive(true);
            }
        }

        private void GenerateButtons()
        {
            // 유니모 버튼 생성
            for (int i = 0; i < _unimoSOs.Length; i++)
            {
                UnimoSO so = _unimoSOs[i];
                if (so == null) continue;

                BuyButtonBehaviour button = Instantiate(_buyButtonPrefab, _unimoParent);
                button.name = $"UnimoButton_{so.name}";
                button.SetupUnimo(so, _infoText);

                _unimoDict.Add(so.Index, button);
            }

            // 카트 버튼 생성
            for (int i = 0; i < _kartSOs.Length; i++)
            {
                KartSO so = _kartSOs[i];
                if (so == null) continue;

                BuyButtonBehaviour button = Instantiate(_buyButtonPrefab, _kartParent);
                button.name = $"KartButton_{so.name}";
                button.SetupKart(so, _infoText);

                _kartDict.Add(so.Index, button);
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
            // key: int index, value: int level, 0 혹은 null이면 미획득, 1 이상은 획득 및 강화 상태
            Dictionary<int, int> levels = new();

            if (snap != null && snap.Exists)
            {
                foreach (var child in snap.Children)
                {
                    int index;
                    if (!int.TryParse(child.Key, out index)) continue;

                    int level = 0;
                    if (child.Value != null)
                    {
                        int.TryParse(child.Value.ToString(), out level);
                    }

                    levels[index] = level;
                }
            }

            foreach (KeyValuePair<int, BuyButtonBehaviour> kv in _unimoDict)
            {
                int level;
                bool owned = levels.TryGetValue(kv.Key, out level) && level > 0;
                kv.Value.SetOwnedVisual(owned);
            }
        }

        // 카트 변화 콜백
        private void OnKartInventorySnapshot(DataSnapshot snap)
        {
            // key: int index, value: int level, 0 혹은 null이면 미획득, 1 이상은 획득 및 강화 상태
            Dictionary<int, int> levels = new();

            if (snap != null && snap.Exists)
            {
                foreach (var child in snap.Children)
                {
                    int index;
                    if (!int.TryParse(child.Key, out index)) continue;

                    int level = 0;
                    if (child.Value != null)
                    {
                        int.TryParse(child.Value.ToString(), out level);
                    }

                    levels[index] = level;
                }
            }

            foreach (KeyValuePair<int, BuyButtonBehaviour> kv in _kartDict)
            {
                int level;
                bool owned = levels.TryGetValue(kv.Key, out level) && level > 0;
                kv.Value.SetOwnedVisual(owned);
            }
        }

        #endregion
    }
}
