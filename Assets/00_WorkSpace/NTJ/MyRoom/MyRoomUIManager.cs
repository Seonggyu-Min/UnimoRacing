using MSG;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PopupBase를 상속받아 팝업 기능을 통합
public class MyRoomUIManager : MonoBehaviour
{
    [Header("UI 패널")]
    [SerializeField] private GameObject myRoom1Panel;
    [SerializeField] private GameObject myRoom2Panel;

    [Header("마이룸1 UI")]
    //[SerializeField] private RawImage myRoom1CharacterImage;
    //[SerializeField] private RawImage myRoom1KartImage;
    [SerializeField] private TMP_Text myRoom1CharacterNameText;
    [SerializeField] private TMP_Text myRoom1CharacterDescText;
    [SerializeField] private TMP_Text myRoom1KartNameText;
    [SerializeField] private TMP_Text myRoom1KartLevelText;
    [SerializeField] private TMP_Text myRoom1KartDescText;
    [SerializeField] private Button changeButton;

    [Header("마이룸2 UI")]
    //[SerializeField] private RawImage myRoom2CharacterImage;
    //[SerializeField] private RawImage myRoom2KartImage;
    [SerializeField] private TMP_Text myRoom2KartDescText;
    [SerializeField] private TMP_Text myRoom2CharacterDescText;

    [Header("인벤토리 패널")]
    [SerializeField] private GameObject characterInventoryPanel;
    [SerializeField] private GameObject carInventoryPanel;
    [SerializeField] private Toggle characterToggleButton;
    [SerializeField] private Toggle carToggleButton;

    [Header("인벤토리 부모 및 프리팹")]
    [SerializeField] private Transform kartInventoryParent;
    [SerializeField] private GameObject kartInventoryPrefab;
    [SerializeField] private Transform characterInventoryParent;
    [SerializeField] private GameObject characterInventoryPrefab;

    [Header("스크롤뷰 최적화")]
    [SerializeField] private ScrollItemChecker2 _unimoScrollItemChecker2;
    [SerializeField] private ScrollRect _unimoScrollRect;
    [SerializeField] private ScrollItemChecker2 _kartScrollItemChecker2;
    [SerializeField] private ScrollRect _kartScrollRect;

    private Dictionary<int, IPreviewItem> _unimoDict = new();
    private Dictionary<int, IPreviewItem> _kartDict = new();

    // 이 메서드를 외부에서 호출하여 마이룸 패널을 엽니다.
    public void ShowMyRoomPanel()
    {
        SetPanel(true);
        SetInventoryPanel(true);
        characterToggleButton.isOn = true;
        
        // MyRoomManager의 모든 UI 업데이트를 트리거합니다.
       // MyRoomManager.Instance.UpdateAllUI();
    }

    // 다른 팝업들처럼 닫는 메서드도 추가할 수 있습니다.
    public void HideMyRoomPanel()
    {
        myRoom1Panel.SetActive(false);
        myRoom2Panel.SetActive(false);
    }

    private void OnEnable()
    {
        MyRoomManager.OnCharacterEquipped += UpdateCharacterUI;
        MyRoomManager.OnKartEquipped += UpdateKartUI;
        MyRoomManager.OnInventoryUpdated += PopulateInventories;
        MyRoomManager.OnKartLevelUpdated += UpdateKartLevelUI;
        MyRoomManager.OnKartStatsUpdated += UpdateKartStatsUI;

        // **OnEnable()에서 패널을 바로 활성화하던 코드를 제거했습니다.**
        // SetPanel(true);
    }

    private void OnDisable()
    {
        MyRoomManager.OnCharacterEquipped -= UpdateCharacterUI;
        MyRoomManager.OnKartEquipped -= UpdateKartUI;
        MyRoomManager.OnInventoryUpdated -= PopulateInventories;
        MyRoomManager.OnKartLevelUpdated -= UpdateKartLevelUI;
        MyRoomManager.OnKartStatsUpdated -= UpdateKartStatsUI;
    }

    private void Start()
    {
        characterToggleButton.onValueChanged.AddListener((isOn) => { if (isOn) SetInventoryPanel(true); });
        carToggleButton.onValueChanged.AddListener((isOn) => { if (isOn) SetInventoryPanel(false); });
        changeButton.onClick.AddListener(() => SetPanel(false));
    }

    public void SetPanel(bool isMyRoom1)
    {
        myRoom1Panel.SetActive(isMyRoom1);
        myRoom2Panel.SetActive(!isMyRoom1);
    }

    private void SetInventoryPanel(bool isCharacterPanel)
    {
        characterInventoryPanel.SetActive(isCharacterPanel);
        carInventoryPanel.SetActive(!isCharacterPanel);
    }

    private void PopulateInventories()
    {
        PopulateKartInventory();
        PopulateCharacterInventory();
    }

    private void PopulateKartInventory()
    {
        _kartDict.Clear();
        foreach (Transform child in kartInventoryParent) Destroy(child.gameObject);
        foreach (var kartData in MyRoomManager.Instance.GetAllKartData())
        {
            GameObject item = Instantiate(kartInventoryPrefab, kartInventoryParent);
            var ui = item.GetComponent<KartInventoryUI>();
            _kartDict.Add(kartData.KartID, ui);
            ui.Init(kartData, MyRoomManager.Instance, MyRoomManager.Instance.IsOwned(kartData));
        }
        if (_kartScrollItemChecker2 != null && _kartScrollRect != null)
        {
            _kartScrollItemChecker2.Register(_kartScrollRect, _kartDict);
        }
    }

    private void PopulateCharacterInventory()
    {
        _unimoDict.Clear();
        foreach (Transform child in characterInventoryParent) Destroy(child.gameObject);
        foreach (var charData in MyRoomManager.Instance.GetAllCharacterData())
        {
            GameObject item = Instantiate(characterInventoryPrefab, characterInventoryParent);
            var ui = item.GetComponent<CharacterInventoryUI>();
            _unimoDict.Add(charData.characterId, ui);
            ui.Init(charData, MyRoomManager.Instance, MyRoomManager.Instance.IsOwned(charData));
        }
        if (_unimoScrollItemChecker2 != null && _unimoScrollRect != null)
        {
            _unimoScrollItemChecker2.Register(_unimoScrollRect, _unimoDict);
        }
    }

    private void UpdateCharacterUI(UnimoCharacterSO character)
    {
        if (character != null && character.characterSprite != null)
        {
            //myRoom1CharacterImage.texture = character.characterSprite.texture;
            //myRoom2CharacterImage.texture = character.characterSprite.texture;
            myRoom1CharacterNameText.text = character.characterName;
            myRoom1CharacterDescText.text = character.characterInfo;
            myRoom2CharacterDescText.text = character.characterInfo;
        }
    }

    private void UpdateKartUI(UnimoKartSO kart)
    {
        if (kart != null && kart.kartSprite != null)
        {
            //myRoom1KartImage.texture = kart.kartSprite.texture;
            //myRoom2KartImage.texture = kart.kartSprite.texture;
            myRoom1KartNameText.text = kart.carName;
            myRoom1KartDescText.text = kart.carDesc;
            myRoom2KartDescText.text = kart.carDesc;
            MyRoomManager.Instance.UpdateKartStatsFromDB();
        }
    }

    private void UpdateKartLevelUI(int level)
    {
        myRoom1KartLevelText.text = $"Lv. {level}";
    }

    private void UpdateKartStatsUI(int attack, int defense)
    {
        // 이 메서드는 MyRoomManager에서 받은 스탯을 사용해 UI를 업데이트하는 로직을 추가하면 됩니다.
    }
}