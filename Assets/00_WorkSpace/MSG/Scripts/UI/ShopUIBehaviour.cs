using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class ShopUIBehaviour : MonoBehaviour
    {
        [SerializeField] private UnimoSO[] _unimoSOs;
        [SerializeField] private KartSO[] _kartSOs;

        [SerializeField] private GameObject _unimoShopPanel;
        [SerializeField] private GameObject _kartShopPanel;

        [SerializeField] private Button _unimoShopButton;
        [SerializeField] private Button _kartShopButton;

        [SerializeField] private Transform _unimoParent;
        [SerializeField] private Transform _kartParent;
        [SerializeField] private BuyButtonBehaviour _buyButtonPrefab;

        private List<BuyButtonBehaviour> _unimoList = new();
        private List<BuyButtonBehaviour> _kartList = new();

        private Dictionary<int, BuyButtonBehaviour> _unimoDict = new();
        private Dictionary<int, BuyButtonBehaviour> _kartDict = new();

        private bool _showUnimoPanel = true;
        private bool _isGenerated = false;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;


        private void OnEnable()
        {
            if (!_isGenerated)
            {
                GenerateButtons();
                _isGenerated = true;
            }

            ShowUnimoShop(_showUnimoPanel);

            // OnValueChanged 구독
        }

        private void OnDisable()
        {
            // OnValueChanged 구독 해제
        }

        public void OnClickUnimoShopButton()
        {
            ShowUnimoShop(true);
        }

        public void OnClickKartShopButton()
        {
            ShowUnimoShop(false);
        }

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
                button.SetupUnimo(so);

                _unimoList.Add(button);
            }

            // 카트 버튼 생성
            for (int i = 0; i < _kartSOs.Length; i++)
            {
                KartSO so = _kartSOs[i];
                if (so == null) continue;

                BuyButtonBehaviour button = Instantiate(_buyButtonPrefab, _kartParent);
                button.name = $"KartButton_{so.name}";
                button.SetupKart(so);

                _kartList.Add(button);
            }
        }

        private void CheckAndRenewOwned()
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.UnimosInventory(CurrentUid), snap =>
            {
                if (!snap.Exists)
                {
                    //_testText.text = "소유한 유니모가 없습니다";
                    return;
                }

                List<string> lines = new();
                foreach (var c in snap.Children)
                {
                    string id = c.Key;
                    string level = c.Value?.ToString() ?? "0";
                    lines.Add($"유니모 id: {id}, 레벨: {level}");
                }
                //_testText.text = string.Join("\n", lines);
            },
            err => Debug.LogWarning($"소유한 유니모 읽기 오류: {err}"));
        }
    }
}
