using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//namespace MSG
//{
//   // ShopManager: ������ UI, ������ ���, �׸��� Firebase ������ ��� �����մϴ�.
//   public class ShopManager : PopupBase
//   {
//       #region Fields and Properties
//       [Header("Register SO")]
//       [SerializeField] private UnimoCharacterSO[] _unimoSOs;
//       [SerializeField] private UnimoKartSO[] _kartSOs;
//
//       [Header("Shop Panel")]
//       [SerializeField] private GameObject _unimoShopPanel;
//       [SerializeField] private GameObject _kartShopPanel;
//
//       [Header("Shop Panel Change Buttons")]
//       [SerializeField] private Toggle _unimoShopToggleButton;
//       [SerializeField] private Toggle _kartShopToggleButton;
//
//       [Header("Button Parent")]
//       [SerializeField] private Transform _unimoParent;
//       [SerializeField] private Transform _kartParent;
//       [SerializeField] private BuyButtonBehaviour _buyButtonPrefab;
//
//       [Header("Info Text")]
//       [SerializeField] private TMP_Text _infoText;
//
//       private readonly Dictionary<int, BuyButtonBehaviour> _unimoDict = new();
//       private readonly Dictionary<int, BuyButtonBehaviour> _kartDict = new();
//       private bool _isGenerated = false;
//
//       private Action _unsubUnimoInv;
//       private Action _unsubKartInv;
//
//       private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;
//       #endregion
//
//       #region Unity Methods
//       private void OnEnable()
//       {
//           _unimoShopPanel.SetActive(_unimoShopToggleButton.isOn);
//           _kartShopPanel.SetActive(_kartShopToggleButton.isOn);
//
//           if (!_isGenerated)
//           {
//               GenerateButtons();
//               _isGenerated = true;
//           }
//           SubscribeInventory();
//       }
//
//       private void OnDisable()
//       {
//           UnsubscribeInventory();
//       }
//
//       private void Start()
//       {
//           _unimoShopToggleButton.onValueChanged.AddListener(isOn => TogglePanel(isOn, _unimoShopPanel));
//           _kartShopToggleButton.onValueChanged.AddListener(isOn => TogglePanel(isOn, _kartShopPanel));
//
//           _unimoShopToggleButton.isOn = true;
//           _kartShopToggleButton.isOn = false;
//       }
//       #endregion
//
//       #region Private Methods
//       private void TogglePanel(bool isOn, GameObject panel)
//       {
//           if (panel == null) return;
//           panel.SetActive(isOn);
//       }
//
//       private void GenerateButtons()
//       {
//           // ���ϸ� ��ư ����
//           for (int i = 0; i < _unimoSOs.Length; i++)
//           {
//               UnimoCharacterSO so = _unimoSOs[i];
//               if (so == null) continue;
//               BuyButtonBehaviour button = Instantiate(_buyButtonPrefab, _unimoParent);
//               button.name = $"UnimoButton_{so.characterName}";
//               button.SetupButton(so.characterName, so.characterSprite, string.Empty);
//               button.SetupTypeAndId(BuyButtonBehaviour.ItemType.Unimo, so.characterId);
//              // _unimoDict.Add(so.characterId, button);
//               button.RefreshItemState(0); // ��� ��ư�� "�̺���" ���·� �ʱ�ȭ
//           }
//
//           // īƮ ��ư ����
//           for (int i = 0; i < _kartSOs.Length; i++)
//           {
//               UnimoKartSO so = _kartSOs[i];
//               if (so == null) continue;
//               BuyButtonBehaviour button = Instantiate(_buyButtonPrefab, _kartParent);
//               button.name = $"KartButton_{so.carName}";
//               button.SetupButton(so.carName, so.kartSprite, string.Empty);
//               button.SetupTypeAndId(BuyButtonBehaviour.ItemType.Kart, so.KartID);
//               _kartDict.Add(so.KartID, button);
//               button.RefreshItemState(0); // ��� ��ư�� "�̺���" ���·� �ʱ�ȭ
//           }
//       }
//
//       private void SubscribeInventory()
//       {
//           _unsubUnimoInv = DatabaseManager.Instance.SubscribeValueChanged(
//               DBRoutes.UnimosInventory(CurrentUid),
//               onChanged: OnUnimoInventorySnapshot,
//               onError: (err) => Debug.LogWarning($"[ShopManager] ���ϸ� �κ��丮 ���� ����: {err}")
//           );
//
//           _unsubKartInv = DatabaseManager.Instance.SubscribeValueChanged(
//               DBRoutes.KartsInventory(CurrentUid),
//               onChanged: OnKartInventorySnapshot,
//               onError: (err) => Debug.LogWarning($"[ShopManager] īƮ �κ��丮 ���� ����: {err}")
//           );
//       }
//
//       private void UnsubscribeInventory()
//       {
//           _unsubUnimoInv?.Invoke();
//           _unsubKartInv?.Invoke();
//           _unsubUnimoInv = null;
//           _unsubKartInv = null;
//       }
//
//       private void OnUnimoInventorySnapshot(DataSnapshot snap)
//       {
//           var inventoryData = snap.Value as Dictionary<string, object> ?? new Dictionary<string, object>();
//
//           // ���� ������ �ִ� ��� ��ư�� �ݺ��մϴ�.
//           foreach (var kv in _unimoDict)
//           {
//               int unimoId = kv.Key;
//               BuyButtonBehaviour button = kv.Value;
//
//               // ���� Unimo ID�� Firebase �����Ϳ� �ִ��� Ȯ���ϼ���.
//               if (inventoryData.ContainsKey(unimoId.ToString()))
//               {
//                   // ���� �ִٸ�, ����ڰ� �����մϴ�. ������ 0���� ū ������ �����ϼ���.
//                   button.RefreshItemState(1); // �������� ��Ÿ������ 1 �̻����� �����ϼ���.
//               }
//               else
//               {
//                   // �ش� �׸��� ������ ����ڰ� �������� ���� ���Դϴ�. ���� 0���� �����ϼ���.
//                   button.RefreshItemState(0);
//               }
//           }
//       }
//
//       private void OnKartInventorySnapshot(DataSnapshot snap)
//       {
//           var inventoryData = snap.Value as Dictionary<string, object> ?? new Dictionary<string, object>();
//
//           foreach (var kv in _kartDict)
//           {
//               int kartId = kv.Key;
//               BuyButtonBehaviour button = kv.Value;
//
//               if (inventoryData.ContainsKey(kartId.ToString()))
//               {
//                   // īƮ�� ��� Firebase���� ���� ������ �����ɴϴ�.
//                   int currentLevel = 0;
//                   if (int.TryParse(inventoryData[kartId.ToString()].ToString(), out int level))
//                   {
//                       currentLevel = level;
//                   }
//                   button.RefreshItemState(currentLevel);
//               }
//               else
//               {
//                   button.RefreshItemState(0);
//               }
//           }
//       }
//       #endregion
//   }
//