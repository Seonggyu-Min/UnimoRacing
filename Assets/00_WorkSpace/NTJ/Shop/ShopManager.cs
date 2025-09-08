using Firebase.Database;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSG
{
    // ShopManager: 상점의 UI, 아이템 목록, 그리고 Firebase 연동을 모두 관리합니다.
    public class ShopManager : PopupBase
    {
        #region Fields and Properties

        [Header("Register SO")]
        [SerializeField] private UnimoSO[] _unimoSOs;
        [SerializeField] private KartSO[] _kartSOs;

        [Header("Shop UI")]
        [SerializeField] private GameObject _unimoShopPanel;
        [SerializeField] private GameObject _kartShopPanel;

        [Header("Shop Toggle Buttons")]
        [SerializeField] private Toggle _unimoShopToggleButton;
        [SerializeField] private Toggle _kartShopToggleButton;

        [Header("Button Parent")]
        [SerializeField] private Transform _unimoParent;
        [SerializeField] private Transform _kartParent;
        [SerializeField] private BuyButtonBehaviour _buyButtonPrefab;

        [Header("Info Text")]
        [SerializeField] private TMP_Text _infoText;
        [SerializeField] private Button _closeButton;

        // 버튼 인덱스로 조회용
        private Dictionary<int, BuyButtonBehaviour> _unimoDict = new();
        private Dictionary<int, BuyButtonBehaviour> _kartDict = new();

        // 패널 및 버튼 UI 상태
        private bool _isGenerated = false;
        private bool _showUnimoPanel = true;

        // 구독 해제 캐싱용 Action
        private Action _unsubUnimoInv;
        private Action _unsubKartInv;

        // 헬퍼 프로퍼티
        private string CurrentUid => FirebaseManager.Instance?.Auth?.CurrentUser?.UserId;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            _unimoShopToggleButton.onValueChanged.AddListener(OnUnimoToggleValueChanged);
            _kartShopToggleButton.onValueChanged.AddListener(OnKartToggleValueChanged);
            // 닫기 버튼 리스너 연결
            if (_closeButton != null)
            {
                // TODO: PopupBase를 사용하지 않는다면, 이 오브젝트를 비활성화하는 로직으로 변경
                _closeButton.onClick.AddListener(() => this.gameObject.SetActive(false));
            }
        }

        private void OnEnable()
        {
            // 패널이 활성화될 때
            if (!_isGenerated)
            {
                GenerateButtons();
                _isGenerated = true;
            }

            // UI 상태 초기화
            SetShopPanel(_showUnimoPanel);

            // Firebase 구독 시작
            SubscribeInventory();
        }

        private void OnDisable()
        {
            // 패널이 비활성화될 때 Firebase 구독 해제
            UnsubscribeInventory();
        }

        #endregion

        #region Public UI Methods

        /// <summary>
        /// 상점 패널을 전환하고, 토글 버튼의 색상을 변경합니다.
        /// </summary>
        /// <param name="isUnimoPanel">유니모 패널을 보여줄지 여부</param>
        public void SetShopPanel(bool isUnimoPanel)
        {
            _showUnimoPanel = isUnimoPanel;

            _unimoShopPanel.SetActive(isUnimoPanel);
            _kartShopPanel.SetActive(!isUnimoPanel);

            // 토글 버튼의 상호작용 가능 여부와 색상 변경 (버튼의 색상 변경 로직은 UI 측에서 처리 권장)
            _unimoShopToggleButton.interactable = !isUnimoPanel;
            _kartShopToggleButton.interactable = isUnimoPanel;
        }

        #endregion

        #region Private Data/Core Logic

        private void GenerateButtons()
        {
            // 유니모 버튼 생성
            foreach (UnimoSO so in _unimoSOs)
            {
                if (so == null) continue;
                BuyButtonBehaviour button = Instantiate(_buyButtonPrefab, _unimoParent);
                button.name = $"UnimoButton_{so.name}";
                button.SetupUnimo(so, _infoText);
                _unimoDict.Add(so.Index, button);
            }

            // 카트 버튼 생성
            foreach (KartSO so in _kartSOs)
            {
                if (so == null) continue;
                BuyButtonBehaviour button = Instantiate(_buyButtonPrefab, _kartParent);
                button.name = $"KartButton_{so.name}";
                button.SetupKart(so, _infoText);
                _kartDict.Add(so.Index, button);
            }
        }

        private void SubscribeInventory()
        {
            if (string.IsNullOrEmpty(CurrentUid)) return;

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

        // 유니모 변화 콜백
        private void OnUnimoInventorySnapshot(DataSnapshot snap)
        {
            Dictionary<int, int> levels = new();
            if (snap != null && snap.Exists)
            {
                foreach (var child in snap.Children)
                {
                    if (!int.TryParse(child.Key, out int index) || child.Value == null) continue;
                    int.TryParse(child.Value.ToString(), out int level);
                    levels[index] = level;
                }
            }

            foreach (KeyValuePair<int, BuyButtonBehaviour> kv in _unimoDict)
            {
                // 0, 1, 2번 인덱스는 기본 아이템으로 간주
                bool isDefaultItem = kv.Key <= 2;

                // 데이터베이스에 존재하거나 기본 아이템이면 '소유'로 처리
                bool owned = levels.TryGetValue(kv.Key, out int level) && level > 0;
                owned = owned || isDefaultItem; // 기본 아이템이라면 항상 true

                kv.Value.SetOwnedVisual(owned);
            }
        }

        // 카트 변화 콜백
        private void OnKartInventorySnapshot(DataSnapshot snap)
        {
            Dictionary<int, int> levels = new();
            if (snap != null && snap.Exists)
            {
                foreach (var child in snap.Children)
                {
                    if (!int.TryParse(child.Key, out int index) || child.Value == null) continue;
                    int.TryParse(child.Value.ToString(), out int level);
                    levels[index] = level;
                }
            }

            foreach (KeyValuePair<int, BuyButtonBehaviour> kv in _kartDict)
            {
                // 0, 1, 2번 인덱스는 기본 카트로 간주
                bool isDefaultItem = kv.Key <= 2;

                bool owned = levels.TryGetValue(kv.Key, out int level) && level > 0;
                owned = owned || isDefaultItem; // 기본 아이템이라면 항상 true

                kv.Value.SetOwnedVisual(owned);
            }
        }

        #endregion
        private void OnUnimoToggleValueChanged(bool isOn)
        {
            _unimoShopPanel.SetActive(isOn);
        }

        private void OnKartToggleValueChanged(bool isOn)
        {
            _kartShopPanel.SetActive(isOn);
        }
    }

}