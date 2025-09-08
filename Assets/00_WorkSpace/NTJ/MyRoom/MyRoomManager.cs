using DA_Assets.FCU.Model;
using Firebase.Database;
using MSG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;


public class MyRoomManager : MonoBehaviour
{
    [SerializeField] private List<UnimoKartSO> allKartData;
    [SerializeField] private List<UnimoCharacterSO> allCharacterData;

    // UI 관리 스크립트 참조
    [SerializeField] private MyRoomUIManager uiManager;

    // 인벤토리 UI를 생성할 부모와 프리팹
    [SerializeField] private Transform kartInventoryParent;
    [SerializeField] private GameObject kartInventoryPrefab;
    [SerializeField] private Transform characterInventoryParent;
    [SerializeField] private GameObject characterInventoryPrefab;

    private UnimoKartSO currentEquippedKart;
    private UnimoCharacterSO currentEquippedCharacter;

    // 상단에 표시될 2D 이미지용 Image 컴포넌트
    [Header("Display Images")]
    [SerializeField] private Image characterDisplayImage;
    [SerializeField] private Image kartDisplayImage;

    [Header("Item Descriptions")]
    [SerializeField] private TMP_Text kartDescText;
    [SerializeField] private TMP_Text characterDescText;
    [SerializeField] private TMP_Text passiveSkillIdText;

    private string CurrentUid => FirebaseManager.Instance?.Auth?.CurrentUser?.UserId;
    public Sprite CurrentEquippedCharacterSprite => currentEquippedCharacter.characterSprite;
    public Sprite CurrentEquippedKartSprite => currentEquippedKart.kartSprite;

    public static event Action OnMyRoomPanelClosed;

    private Action _unsubUnimoInv;
    private Action _unsubKartInv;
    private Dictionary<string, object> _ownedKarts;
    private Dictionary<string, object> _ownedCharacters;

    #region MyRoom

    private void Start()
    {
        // UI 관리 스크립트 참조 가져오기
        uiManager = GetComponent<MyRoomUIManager>();

        // 인벤토리 채우기
        PopulateKartInventory();
        PopulateCharacterInventory();

        // 게임 시작 시, 저장된 아이템 불러오기
        LoadEquippedItems();
    }

    // 아이템을 Firebase에서 불러와 장착하는 함수
    private void LoadEquippedItems()
    {
        if (string.IsNullOrEmpty(CurrentUid))
        {
            // 유저 정보가 없으면 기본 아이템 장착 후 종료
            if (allKartData.Count > 0) EquipKartInternal(allKartData[0]);
            if (allCharacterData.Count > 0) EquipCharacterInternal(allCharacterData[0]);
            return;
        }

        // 장착된 카트 불러오기
        DatabaseManager.Instance.GetOnMain(
            DBRoutes.EquippedKart(CurrentUid),
            onSuccess: (snapshot) =>
            {
                if (snapshot.Exists && snapshot.Value != null)
                {
                    if (int.TryParse(snapshot.Value.ToString(), out int kartId))
                    {
                        UnimoKartSO savedKart = allKartData.FirstOrDefault(k => k.carId == kartId);
                        if (savedKart != null)
                        {
                            EquipKartInternal(savedKart);
                        }
                    }
                }
                else
                {
                    // 저장된 데이터가 없으면 기본 아이템 장착
                    if (allKartData.Count > 0) EquipKartInternal(allKartData[0]);
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"카트 데이터 불러오기 실패: {error}");
                // 실패 시 기본 아이템 장착
                if (allKartData.Count > 0) EquipKartInternal(allKartData[0]);
            }
        );

        // 장착된 캐릭터 불러오기
        DatabaseManager.Instance.GetOnMain(
            DBRoutes.EquippedUnimo(CurrentUid),
            onSuccess: (snapshot) =>
            {
                if (snapshot.Exists && snapshot.Value != null)
                {
                    if (int.TryParse(snapshot.Value.ToString(), out int characterId))
                    {
                        UnimoCharacterSO savedCharacter = allCharacterData.FirstOrDefault(c => c.characterId == characterId);
                        if (savedCharacter != null)
                        {
                            EquipCharacterInternal(savedCharacter);
                        }
                    }
                }
                else
                {
                    // 저장된 데이터가 없으면 기본 아이템 장착
                    if (allCharacterData.Count > 0) EquipCharacterInternal(allCharacterData[0]);
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"캐릭터 데이터 불러오기 실패: {error}");
                // 실패 시 기본 아이템 장착
                if (allCharacterData.Count > 0) EquipCharacterInternal(allCharacterData[0]);
            }
        );
    }

    // UI에서 아이템을 클릭할 때 호출되는 공개 메서드
    public void EquipKart(UnimoKartSO kart)
    {
        // Check ownership before equipping
        if (!IsDefaultOwned(kart) && !(_ownedKarts != null && _ownedKarts.ContainsKey(kart.carId.ToString())))
        {
            Debug.Log("You do not own this kart.");
            return;
        }
        EquipKartInternal(kart);
        SaveAndReloadItems();
    }

    public void EquipCharacter(UnimoCharacterSO character)
    {
        // Check ownership before equipping
        if (!IsDefaultOwned(character) && !(_ownedCharacters != null && _ownedCharacters.ContainsKey(character.characterId.ToString())))
        {
            Debug.Log("You do not own this character.");
            return;
        }
        EquipCharacterInternal(character);
        SaveAndReloadItems();
    }

    // 내부에서만 호출되는 장착 로직
    private void EquipKartInternal(UnimoKartSO kart)
    {
        currentEquippedKart = kart;
        if (kartDisplayImage != null && kart.kartSprite != null)
        {
            kartDisplayImage.sprite = kart.kartSprite;
            kartDisplayImage.enabled = true;
        }
        if (kartDescText != null)
        {
            kartDescText.text = kart.carDesc;
        }
        if (passiveSkillIdText != null)
        {
            passiveSkillIdText.text = "Passive Skill ID: " + kart.passiveSkillId.ToString();
        }
        UpdateEquippedUI();
    }

    private void EquipCharacterInternal(UnimoCharacterSO character)
    {
        currentEquippedCharacter = character;
        if (characterDisplayImage != null && character.characterSprite != null)
        {
            characterDisplayImage.sprite = character.characterSprite;
            characterDisplayImage.enabled = true;
        }
        UpdateEquippedUI();
    }

    // Firebase에 장착 아이템을 저장하는 함수

    private void SaveAndReloadItems()
    {
        if (string.IsNullOrEmpty(CurrentUid)) return;
        if (currentEquippedKart == null || currentEquippedCharacter == null) return;

        var updates = new Dictionary<string, object>
        {
            { DBRoutes.EquippedKart(CurrentUid), currentEquippedKart.carId },
            { DBRoutes.EquippedUnimo(CurrentUid), currentEquippedCharacter.characterId }
        };

        DatabaseManager.Instance.UpdateOnMain(updates,
            onSuccess: () =>
            {
                Debug.Log("장착 아이템 저장 완료. 홈 화면 UI 업데이트 요청.");
                // Direct call to HomeManager's public method
                HomeManager.Instance.LoadAndEquipItems();
            },
            onError: err => Debug.LogError($"장착 아이템 저장 실패: {err}")
        );
    }


    private void UpdateEquippedUI()
    {
        uiManager.UpdateEquippedUI(
            currentEquippedCharacter.characterSprite,
            currentEquippedCharacter.characterName,
            currentEquippedKart.kartSprite,
            currentEquippedKart.carName
        );
    }

    public void PopulateKartInventory()
    {
        foreach (Transform child in kartInventoryParent) Destroy(child.gameObject);

        foreach (var kartData in allKartData)
        {
            GameObject item = Instantiate(kartInventoryPrefab, kartInventoryParent);
            var ui = item.GetComponent<KartInventoryUI>();

            bool isOwned = (_ownedKarts != null && _ownedKarts.ContainsKey(kartData.carId.ToString())); // || IsDefaultOwned(kartData);

            // Pass ownership status to your UI component
            ui.Init(kartData, this, isOwned);
        }
    }

    public void PopulateCharacterInventory()
    {
        foreach (Transform child in characterInventoryParent) Destroy(child.gameObject);

        foreach (var charData in allCharacterData)
        {
            GameObject item = Instantiate(characterInventoryPrefab, characterInventoryParent);
            var ui = item.GetComponent<CharacterInventoryUI>();

            bool isOwned = (_ownedCharacters != null && _ownedCharacters.ContainsKey(charData.characterId.ToString()));// || IsDefaultOwned(charData);

            // Pass ownership status to your UI component
            ui.Init(charData, this, isOwned);
        }
    }
    #endregion

    private void OnEnable()
    {
        // Subscribe to inventory changes when the MyRoom panel is active
        SubscribeToInventoryChanges();
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        UnsubscribeFromInventoryChanges();
    }

    private void SubscribeToInventoryChanges()
    {
        _unsubUnimoInv = DatabaseManager.Instance.SubscribeValueChanged(
            DBRoutes.UnimosInventory(CurrentUid),
            onChanged: OnUnimoInventoryChanged,
            onError: (err) => Debug.LogWarning($"[MyRoomManager] Unimo inventory subscription error: {err}")
        );
        _unsubKartInv = DatabaseManager.Instance.SubscribeValueChanged(
            DBRoutes.KartsInventory(CurrentUid),
            onChanged: OnKartInventoryChanged,
            onError: (err) => Debug.LogWarning($"[MyRoomManager] Kart inventory subscription error: {err}")
        );
    }

    private void UnsubscribeFromInventoryChanges()
    {
        _unsubUnimoInv?.Invoke();
        _unsubKartInv?.Invoke();
        _unsubUnimoInv = null;
        _unsubKartInv = null;
    }

    private void OnUnimoInventoryChanged(DataSnapshot snapshot)
    {
        // C# 9.0+ syntax: `as` operator will return null if cast fails.
        _ownedCharacters = snapshot.Value as Dictionary<string, object>;
        PopulateCharacterInventory();
        LoadEquippedItems(); // Reload equipped items to update the UI
    }

    private void OnKartInventoryChanged(DataSnapshot snapshot)
    {
        _ownedKarts = snapshot.Value as Dictionary<string, object>;
        PopulateKartInventory();
        LoadEquippedItems(); // Reload equipped items to update the UI
    }
   
    private bool IsDefaultOwned(UnimoCharacterSO character)
    {
        return character.characterId >= 20001 && character.characterId <= 20003;
    }

    private bool IsDefaultOwned(UnimoKartSO kart)
    {
        return kart.carId >= 10001 && kart.carId <= 10003;
    }
}