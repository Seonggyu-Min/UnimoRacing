using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MSG;
using Firebase.Database;

public class HomeManager : Singleton<HomeManager>
{
    [SerializeField] private List<UnimoKartSO> allKartData;
    [SerializeField] private List<UnimoCharacterSO> allCharacterData;

    [Header("Display Images")]
    [SerializeField] private Image characterDisplayImage;
    [SerializeField] private Image kartDisplayImage;

    private string CurrentUid => FirebaseManager.Instance?.Auth?.CurrentUser?.UserId;

    private void Awake()
    {
        SingletonInit();
    }

    private void OnEnable()
    {
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsReady)
        {
            LoadAndEquipItems();
        }
        else
        {
            FirebaseManager.Instance.OnFirebaseReady += OnFirebaseReady;
        }
    }

    private void OnDisable()
    {
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.OnFirebaseReady -= OnFirebaseReady;
        }
    }

    private void OnFirebaseReady()
    {
        Debug.Log("Firebase is ready! Starting to load items.");
        LoadAndEquipItems();
    }

    public void LoadAndEquipItems()
    {
        if (string.IsNullOrEmpty(CurrentUid))
        {
            Debug.LogError("User not logged in.");
            return;
        }

        DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedKart(CurrentUid), OnKartLoaded, OnLoadError);
        DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedUnimo(CurrentUid), OnCharacterLoaded, OnLoadError);
    }

    private void OnKartLoaded(DataSnapshot snapshot)
    {
        Debug.Log($"Firebase���� īƮ ������ �ҷ����� �õ�. ���: {DBRoutes.EquippedKart(CurrentUid)}");
        if (snapshot.Exists && snapshot.Value != null)
        {
            Debug.Log($"īƮ ������ �ε� ����! ��: {snapshot.Value}");
            if (int.TryParse(snapshot.Value.ToString(), out int kartId))
            {
                UnimoKartSO savedKart = allKartData.FirstOrDefault(k => k.carId == kartId);
                if (savedKart != null)
                {
                    UpdateKartUI(savedKart);
                    Debug.Log($"īƮ UI ������Ʈ ����: {savedKart.carName}");
                }
            }
        }
        else
        {
            Debug.Log("����� īƮ �����Ͱ� �����ϴ�.");
            if (allKartData.Count > 0) UpdateKartUI(allKartData[0]);
        }
    }

    private void OnCharacterLoaded(DataSnapshot snapshot)
    {
        Debug.Log($"Firebase���� ĳ���� ������ �ҷ����� �õ�. ���: {DBRoutes.EquippedUnimo(CurrentUid)}");
        if (snapshot.Exists && snapshot.Value != null)
        {
            Debug.Log($"ĳ���� ������ �ε� ����! ��: {snapshot.Value}");
            if (int.TryParse(snapshot.Value.ToString(), out int characterId))
            {
                UnimoCharacterSO savedCharacter = allCharacterData.FirstOrDefault(c => c.characterId == characterId);
                if (savedCharacter != null)
                {
                    UpdateCharacterUI(savedCharacter);
                    Debug.Log($"ĳ���� UI ������Ʈ ����: {savedCharacter.characterName}");
                }
            }
        }
        else
        {
            Debug.Log("����� ĳ���� �����Ͱ� �����ϴ�.");
            if (allCharacterData.Count > 0) UpdateCharacterUI(allCharacterData[0]);
        }
    }

    private void OnLoadError(string error)
    {
        Debug.LogError($"Firebase ������ �ҷ����� ����: {error}");
    }

    private void UpdateKartUI(UnimoKartSO kart)
    {
        if (kartDisplayImage != null && kart.kartSprite != null)
        {
            kartDisplayImage.sprite = kart.kartSprite;
            kartDisplayImage.enabled = true;
        }
    }

    private void UpdateCharacterUI(UnimoCharacterSO character)
    {
        if (characterDisplayImage != null && character.characterSprite != null)
        {
            characterDisplayImage.sprite = character.characterSprite;
            characterDisplayImage.enabled = true;
        }
    }
}
