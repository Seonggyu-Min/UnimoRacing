using Firebase.Database;
using MSG;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// MoneyType 및 DBRoutes는 게임에 맞게 정의되어 있어야 합니다.

public class MyRoomManager : MonoBehaviour
{
    // 스크립터블 오브젝트 데이터
    [SerializeField] private List<UnimoCharacterSO> allCharacterData;
    [SerializeField] private List<UnimoKartSO> allKartData;

    // 현재 장착된 아이템 데이터 (데이터 전용)
    private UnimoKartSO _currentEquippedKart;
    private UnimoCharacterSO _currentEquippedCharacter;

    // 인벤토리 데이터 (Firebase에서 동기화)
    private Dictionary<string, object> _ownedKarts;
    private Dictionary<string, object> _ownedCharacters;

    // UI에 변경사항을 알리는 이벤트
    public static event Action<UnimoCharacterSO> OnCharacterEquipped;
    public static event Action<UnimoKartSO> OnKartEquipped;
    public static event Action OnInventoryUpdated;

    // 강화 시 레벨 및 스탯 변경 사항을 알리는 이벤트
    public static event Action<int, int> OnKartStatsUpdated;
    public static event Action<int> OnKartLevelUpdated;

    [SerializeField] private UpgradeButtonBehaviour upgradeButton;

    private string CurrentUid => FirebaseManager.Instance?.Auth?.CurrentUser?.UserId;

    public static MyRoomManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadEquippedItems();
    }

    private void OnEnable()
    {
        SubscribeToInventoryChanges();
    }

    private void OnDisable()
    {
        UnsubscribeFromInventoryChanges();
    }

    #region Equip & Load

    public void ShowKartDetails(int kartId)
    {
        // 1. 필요한 아이템 ID를 UpgradeButtonBehaviour에 설정
        upgradeButton.InitializeButton(kartId);

        // 2. 다른 UI 패널 등 초기화
        // ...
    }

    public void LoadEquippedItems()
    {
        if (string.IsNullOrEmpty(CurrentUid))
        {
            if (allKartData.Count > 0) EquipKartInternal(allKartData[0]);
            if (allCharacterData.Count > 0) EquipCharacterInternal(allCharacterData[0]);
            return;
        }

        DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedKart(CurrentUid),
            onSuccess: (snapshot) =>
            {
                if (snapshot.Exists && int.TryParse(snapshot.Value.ToString(), out int kartId))
                {
                    UnimoKartSO savedKart = allKartData.FirstOrDefault(k => k.KartID == kartId);
                    if (savedKart != null) EquipKartInternal(savedKart);
                }
                else
                {
                    if (allKartData.Count > 0) EquipKartInternal(allKartData[0]);
                }
            }, onError: (err) => Debug.LogError($"카트 데이터 불러오기 실패: {err}")
        );

        DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedUnimo(CurrentUid),
            onSuccess: (snapshot) =>
            {
                if (snapshot.Exists && int.TryParse(snapshot.Value.ToString(), out int characterId))
                {
                    UnimoCharacterSO savedCharacter = allCharacterData.FirstOrDefault(c => c.characterId == characterId);
                    if (savedCharacter != null) EquipCharacterInternal(savedCharacter);
                }
                else
                {
                    if (allCharacterData.Count > 0) EquipCharacterInternal(allCharacterData[0]);
                }
            }, onError: (err) => Debug.LogError($"캐릭터 데이터 불러오기 실패: {err}")
        );
    }

    public void EquipKart(UnimoKartSO kart)
    {
        if (!IsOwned(kart))
        {
            Debug.Log("이 카트를 소유하고 있지 않습니다.");
            return;
        }
        EquipKartInternal(kart);
        SaveEquippedItems();
        UpdateKartStatsFromDB();
        MissionService.Instance.Report(MissionVerb.Change, MissionObject.Engine, false, 1);
    }

    public void EquipCharacter(UnimoCharacterSO character)
    {
        if (!IsOwned(character))
        {
            Debug.Log("이 캐릭터를 소유하고 있지 않습니다.");
            return;
        }
        EquipCharacterInternal(character);
        SaveEquippedItems();
        MissionService.Instance.Report(MissionVerb.Change, MissionObject.Unimo, false, 1);
    }

    private void EquipKartInternal(UnimoKartSO kart)
    {
        _currentEquippedKart = kart;
        upgradeButton.InitializeButton(_currentEquippedKart.KartID);
        OnKartEquipped?.Invoke(kart);
    }
    private void EquipCharacterInternal(UnimoCharacterSO character)
    {
        _currentEquippedCharacter = character;
        OnCharacterEquipped?.Invoke(character);

        int relation = _currentEquippedCharacter.relationCharacterId;
        UnimoKartDatabase.Instance.TryGetByUnimoIndex(relation, out UnimoCharacterSO related);

        // 여기서 인덱스가 있으니 그걸 활용하면 됩니다.
        // Firebase DB에 요청, 아니면 SO에 직접 접근 등을 통해 정보를 얻어올 수 있습니다.
        // ItemPreviewManager.Instance.BindUnimoPreview(); 이거는 근데 나중에 같이 보겠습니다.
    }

    private void SaveEquippedItems()
    {
        if (string.IsNullOrEmpty(CurrentUid) || _currentEquippedKart == null || _currentEquippedCharacter == null) return;
        var updates = new Dictionary<string, object>
        {
            { DBRoutes.EquippedKart(CurrentUid), _currentEquippedKart.KartID },
            { DBRoutes.EquippedUnimo(CurrentUid), _currentEquippedCharacter.characterId }
        };
        DatabaseManager.Instance.UpdateOnMain(updates,
            onSuccess: () => Debug.Log("장착 아이템 저장 완료."),
            onError: err => Debug.LogError($"장착 아이템 저장 실패: {err}")
        );
    }

    private Action _unsubUnimoInv;
    private Action _unsubKartInv;

    private void SubscribeToInventoryChanges()
    {
        if (string.IsNullOrEmpty(CurrentUid)) return;
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
        _ownedCharacters = snapshot.Value as Dictionary<string, object>;
        OnInventoryUpdated?.Invoke();
        LoadEquippedItems();
    }
    private void OnKartInventoryChanged(DataSnapshot snapshot)
    {
        _ownedKarts = snapshot.Value as Dictionary<string, object>;
        OnInventoryUpdated?.Invoke();
        LoadEquippedItems();
    }

    public bool IsOwned(UnimoCharacterSO character) => (_ownedCharacters != null && _ownedCharacters.ContainsKey(character.characterId.ToString()));
    public bool IsOwned(UnimoKartSO kart) => (_ownedKarts != null && _ownedKarts.ContainsKey(kart.KartID.ToString()));

    public List<UnimoCharacterSO> GetAllCharacterData() => allCharacterData;
    public List<UnimoKartSO> GetAllKartData() => allKartData;

    #endregion

    #region Enhancement Logic

    public void RequestKartEnhance()
    {
        if (_currentEquippedKart == null)
        {
            Debug.LogWarning("강화할 카트가 없습니다.");
            return;
        }

        int kartId = _currentEquippedKart.KartID;
        string userId = CurrentUid;

        DatabaseManager.Instance.GetOnMain(DBRoutes.KartInventory(userId, kartId),
            snap =>
            {
                int currentLevel = 0;
                if (snap.Exists && snap.Value != null)
                {
                    int.TryParse(snap.Value.ToString(), out currentLevel);
                }

                int nextLevel = currentLevel + 1;
                int maxLevel = 10; // TODO: 최대 레벨은 별도 관리

                if (currentLevel >= maxLevel)
                {
                    Debug.Log("최대 레벨에 도달했습니다.");
                    return;
                }

                // TODO: 강화 비용 설정 (기존 PatchService 로직 대체)
                int upgradeCost = 100 + (currentLevel * 50); // 예시: 레벨에 따라 비용 증가
                MoneyType costType = MoneyType.Gold; // 예시: 골드로 강화

                // 재화 확인 및 차감 트랜잭션 로직
                TrySpendTransaction(
                    costType,
                    upgradeCost,
                    onDone =>
                    {
                        if (!onDone)
                        {
                            Debug.LogError("재화가 부족하거나 트랜잭션 실패");
                            return;
                        }

                        // Firebase에 레벨 업데이트
                        DatabaseManager.Instance.SetOnMain(
                            DBRoutes.KartInventory(userId, kartId),
                            nextLevel,
                            () =>
                            {
                                Debug.Log($"카트 {_currentEquippedKart.carName} 강화 성공! 새 레벨: {nextLevel}");
                                UpdateKartStatsFromDB();
                            },
                            err => Debug.LogError($"강화 데이터 저장 실패: {err}")
                        );
                    }
                );
            },
            err => Debug.LogError($"인벤토리 데이터 불러오기 실패: {err}")
        );
    }

    public void UpdateKartStatsFromDB()
    {
        if (_currentEquippedKart == null) return;

        int kartId = _currentEquippedKart.KartID;
        string userId = CurrentUid;

        DatabaseManager.Instance.GetOnMain(DBRoutes.KartInventory(userId, kartId),
            snap =>
            {
                int level = 0;
                if (snap.Exists && snap.Value != null)
                {
                    int.TryParse(snap.Value.ToString(), out level);
                }

                // TODO: 스탯 계산식 (임시 데이터)
                int baseAttack = 10;
                int baseDefense = 5;
                int attackIncrease = 2; // 레벨당 공격력 증가
                int defenseIncrease = 1; // 레벨당 방어력 증가

                int currentAttack = baseAttack + (level * attackIncrease);
                int currentDefense = baseDefense + (level * defenseIncrease);

                OnKartLevelUpdated?.Invoke(level);
                OnKartStatsUpdated?.Invoke(currentAttack, currentDefense);
            },
            err => Debug.LogError($"카트 스탯 불러오기 실패: {err}")
        );
    }

    private void TrySpendTransaction(MoneyType moneyType, int price, Action<bool> onDone)
    {
        string path = moneyType switch
        {
            MoneyType.Gold => DBRoutes.Gold(CurrentUid),
            MoneyType.BlueHoneyGem => DBRoutes.BlueHoneyGem(CurrentUid),
            _ => null
        };

        if (path == null)
        {
            onDone?.Invoke(false);
            return;
        }

        DatabaseManager.Instance.RunTransactionOnMain(
            path,
            mutable =>
            {
                long current = 0;
                try
                {
                    if (mutable.Value != null)
                    {
                        current = Convert.ToInt64(mutable.Value);
                    }
                }
                catch
                {
                    Debug.LogError("트랜잭션: 데이터 파싱 실패. Aborting.");
                    return TransactionResult.Abort();
                }

                // 잔액 부족 확인은 한 번만 수행합니다.
                if (current < price)
                {
                    Debug.LogWarning("트랜잭션: 잔액 부족. Aborting.");
                    return TransactionResult.Abort();
                }

                mutable.Value = current - price;
                return TransactionResult.Success(mutable);
            },
            // onSuccess, onError 콜백은 기존과 동일합니다.
            snap => onDone?.Invoke(true),
            errMsg => {
                Debug.LogError($"트랜잭션 실패: {errMsg}");
                onDone?.Invoke(false);
            }
        );
        #endregion
    }
}