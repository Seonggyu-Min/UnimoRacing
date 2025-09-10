using Firebase.Database;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSG
{
    // ShopManager: ������ UI, ������ ���, �׸��� Firebase ������ ��� �����մϴ�.
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

        // ��ư �ε����� ��ȸ��
        private Dictionary<int, BuyButtonBehaviour> _unimoDict = new();
        private Dictionary<int, BuyButtonBehaviour> _kartDict = new();

        // �г� �� ��ư UI ����
        private bool _isGenerated = false;
        private bool _showUnimoPanel = true;

        // ���� ���� ĳ�̿� Action
        private Action _unsubUnimoInv;
        private Action _unsubKartInv;

        // ���� ������Ƽ
        private string CurrentUid => FirebaseManager.Instance?.Auth?.CurrentUser?.UserId;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            _unimoShopToggleButton.onValueChanged.AddListener(OnUnimoToggleValueChanged);
            _kartShopToggleButton.onValueChanged.AddListener(OnKartToggleValueChanged);
            // �ݱ� ��ư ������ ����
            if (_closeButton != null)
            {
                // TODO: PopupBase�� ������� �ʴ´ٸ�, �� ������Ʈ�� ��Ȱ��ȭ�ϴ� �������� ����
                _closeButton.onClick.AddListener(() => this.gameObject.SetActive(false));
            }
        }

        private void OnEnable()
        {
            // �г��� Ȱ��ȭ�� ��
            if (!_isGenerated)
            {
                GenerateButtons();
                _isGenerated = true;
            }

            // UI ���� �ʱ�ȭ
            SetShopPanel(_showUnimoPanel);

            // Firebase ���� ����
            SubscribeInventory();
        }

        private void OnDisable()
        {
            // �г��� ��Ȱ��ȭ�� �� Firebase ���� ����
            UnsubscribeInventory();
        }

        #endregion

        #region Public UI Methods

        /// <summary>
        /// ���� �г��� ��ȯ�ϰ�, ��� ��ư�� ������ �����մϴ�.
        /// </summary>
        /// <param name="isUnimoPanel">���ϸ� �г��� �������� ����</param>
        public void SetShopPanel(bool isUnimoPanel)
        {
            _showUnimoPanel = isUnimoPanel;

            _unimoShopPanel.SetActive(isUnimoPanel);
            _kartShopPanel.SetActive(!isUnimoPanel);

            // ��� ��ư�� ��ȣ�ۿ� ���� ���ο� ���� ���� (��ư�� ���� ���� ������ UI ������ ó�� ����)
            _unimoShopToggleButton.interactable = !isUnimoPanel;
            _kartShopToggleButton.interactable = isUnimoPanel;
        }

        #endregion

        #region Private Data/Core Logic

        private void GenerateButtons()
        {
            // ���ϸ� ��ư ����
            foreach (UnimoSO so in _unimoSOs)
            {
                if (so == null) continue;
                BuyButtonBehaviour button = Instantiate(_buyButtonPrefab, _unimoParent);
                button.name = $"UnimoButton_{so.name}";
                button.SetupUnimo(so, _infoText);
                _unimoDict.Add(so.Index, button);
            }

            // īƮ ��ư ����
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
                onError: (err) => Debug.LogWarning($"[ShopManager] ���ϸ� �κ��丮 ���� ����: {err}")
            );

            _unsubKartInv = DatabaseManager.Instance.SubscribeValueChanged(
                DBRoutes.KartsInventory(CurrentUid),
                onChanged: OnKartInventorySnapshot,
                onError: (err) => Debug.LogWarning($"[ShopManager] īƮ �κ��丮 ���� ����: {err}")
            );
        }

        private void UnsubscribeInventory()
        {
            _unsubUnimoInv?.Invoke();
            _unsubKartInv?.Invoke();
            _unsubUnimoInv = null;
            _unsubKartInv = null;
        }

        // ���ϸ� ��ȭ �ݹ�
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
                // 0, 1, 2�� �ε����� �⺻ ���������� ����
                bool isDefaultItem = kv.Key <= 2;

                // �����ͺ��̽��� �����ϰų� �⺻ �������̸� '����'�� ó��
                bool owned = levels.TryGetValue(kv.Key, out int level) && level > 0;
                owned = owned || isDefaultItem; // �⺻ �������̶�� �׻� true

                kv.Value.SetOwnedVisual(owned);
            }
        }

        // īƮ ��ȭ �ݹ�
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
                // 0, 1, 2�� �ε����� �⺻ īƮ�� ����
                bool isDefaultItem = kv.Key <= 2;

                bool owned = levels.TryGetValue(kv.Key, out int level) && level > 0;
                owned = owned || isDefaultItem; // �⺻ �������̶�� �׻� true

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