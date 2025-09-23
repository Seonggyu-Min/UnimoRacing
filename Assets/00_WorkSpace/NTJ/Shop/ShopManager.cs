using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//namespace MSG
//{
//   // ShopManager: 상점의 UI, 아이템 목록, 그리고 Firebase 연동을 모두 관리합니다.
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
//           // 유니모 버튼 생성
//           for (int i = 0; i < _unimoSOs.Length; i++)
//           {
//               UnimoCharacterSO so = _unimoSOs[i];
//               if (so == null) continue;
//               BuyButtonBehaviour button = Instantiate(_buyButtonPrefab, _unimoParent);
//               button.name = $"UnimoButton_{so.characterName}";
//               button.SetupButton(so.characterName, so.characterSprite, string.Empty);
//               button.SetupTypeAndId(BuyButtonBehaviour.ItemType.Unimo, so.characterId);
//              // _unimoDict.Add(so.characterId, button);
//               button.RefreshItemState(0); // 모든 버튼을 "미보유" 상태로 초기화
//           }
//
//           // 카트 버튼 생성
//           for (int i = 0; i < _kartSOs.Length; i++)
//           {
//               UnimoKartSO so = _kartSOs[i];
//               if (so == null) continue;
//               BuyButtonBehaviour button = Instantiate(_buyButtonPrefab, _kartParent);
//               button.name = $"KartButton_{so.carName}";
//               button.SetupButton(so.carName, so.kartSprite, string.Empty);
//               button.SetupTypeAndId(BuyButtonBehaviour.ItemType.Kart, so.KartID);
//               _kartDict.Add(so.KartID, button);
//               button.RefreshItemState(0); // 모든 버튼을 "미보유" 상태로 초기화
//           }
//       }
//
//       private void SubscribeInventory()
//       {
//           _unsubUnimoInv = DatabaseManager.Instance.SubscribeValueChanged(
//               DBRoutes.UnimosInventory(CurrentUid),
//               onChanged: OnUnimoInventorySnapshot,
//               onError: (err) => Debug.LogWarning($"[ShopManager] 유니모 인벤토리 구독 오류: {err}")
//           );
//
//           _unsubKartInv = DatabaseManager.Instance.SubscribeValueChanged(
//               DBRoutes.KartsInventory(CurrentUid),
//               onChanged: OnKartInventorySnapshot,
//               onError: (err) => Debug.LogWarning($"[ShopManager] 카트 인벤토리 구독 오류: {err}")
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
//           // 로컬 사전에 있는 모든 버튼을 반복합니다.
//           foreach (var kv in _unimoDict)
//           {
//               int unimoId = kv.Key;
//               BuyButtonBehaviour button = kv.Value;
//
//               // 현재 Unimo ID가 Firebase 데이터에 있는지 확인하세요.
//               if (inventoryData.ContainsKey(unimoId.ToString()))
//               {
//                   // 만약 있다면, 사용자가 소유합니다. 레벨을 0보다 큰 값으로 설정하세요.
//                   button.RefreshItemState(1); // 소유권을 나타내려면 1 이상으로 설정하세요.
//               }
//               else
//               {
//                   // 해당 항목이 없으면 사용자가 소유하지 않은 것입니다. 값을 0으로 설정하세요.
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
//                   // 카트의 경우 Firebase에서 실제 레벨을 가져옵니다.
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