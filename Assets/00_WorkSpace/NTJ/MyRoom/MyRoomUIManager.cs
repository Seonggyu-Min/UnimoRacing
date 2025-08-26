using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PopupBase를 상속받아 팝업 기능을 통합
public class MyRoomUIManager : PopupBase
{
    // 장착된 아이템 표시 UI
    [Header("Equipped Items UI")]
    [SerializeField] private Image equippedCharacterImage;
    [SerializeField] private TMP_Text equippedCharacterName;
    [SerializeField] private Image equippedCarImage;
    [SerializeField] private TMP_Text equippedCarName;

    // 인벤토리 패널
    [Header("Inventory Panels")]
    [SerializeField] private GameObject characterInventoryPanel;
    [SerializeField] private GameObject carInventoryPanel;

    // 인벤토리 전환 버튼
    [Header("Toggle Buttons")]
    [SerializeField] private Button characterToggleButton;
    [SerializeField] private Button carToggleButton;

    [SerializeField] private Button closeButton; // 닫기 버튼 추가

    // MyRoomManager 스크립트 참조
    private MyRoomManager myRoomManager;

    private void Start()
    {
        // MyRoomManager 스크립트를 가져와 참조합니다.
        myRoomManager = GetComponent<MyRoomManager>();
        
        // 버튼에 리스너 추가
        characterToggleButton.onClick.AddListener(() => SetInventoryPanel(true));
        carToggleButton.onClick.AddListener(() => SetInventoryPanel(false));
        
        // 닫기 버튼에 Close() 함수 연결
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => Close());
        }

        // 시작 시 캐릭터 인벤토리로 설정
        SetInventoryPanel(true);
    }

    // PopupBase의 Open 메서드를 오버라이드하여 팝업을 엽니다.
    public override void Open()
    {
        base.Open(); // 팝업 오브젝트 활성화
    }

    // 인벤토리 패널 활성화/비활성화
    public void SetInventoryPanel(bool isCharacterPanel)
    {
        // 캐릭터 인벤토리 패널 활성화
        characterInventoryPanel.SetActive(isCharacterPanel);
        // 차량 인벤토리 패널 활성화
        carInventoryPanel.SetActive(!isCharacterPanel);

        // 버튼 상호작용 가능 여부와 색상 변경
        // 활성화된 버튼은 상호작용 불가능하게 만들고, 색상은 활성화 상태로 변경
        characterToggleButton.interactable = !isCharacterPanel;
        carToggleButton.interactable = isCharacterPanel;
    }

    // 장착 UI 업데이트 메서드 (MyRoomManager에서 호출)
    public void UpdateEquippedUI(Sprite characterSprite, string characterName, Sprite carSprite, string carName)
    {
        equippedCharacterImage.sprite = characterSprite;
        equippedCharacterName.text = characterName;
        equippedCarImage.sprite = carSprite;
        equippedCarName.text = carName;
    }
}
