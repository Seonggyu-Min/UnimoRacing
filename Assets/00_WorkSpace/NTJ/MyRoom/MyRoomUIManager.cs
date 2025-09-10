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
    [SerializeField] private Image equippedCarImage;

    // 인벤토리 패널
    [Header("Inventory Panels")]
    [SerializeField] private GameObject characterInventoryPanel;
    [SerializeField] private GameObject carInventoryPanel;

    // 인벤토리 전환 버튼
    [Header("Toggle Buttons")]
    [SerializeField] private Toggle characterToggleButton;
    [SerializeField] private Toggle carToggleButton;

    [SerializeField] private Button closeButton; // 닫기 버튼 추가

    // MyRoomManager 스크립트 참조
    private MyRoomManager myRoomManager;

    private void Start()
    {
        // MyRoomManager 스크립트 참조 가져오기
        myRoomManager = GetComponent<MyRoomManager>();

        // OnValueChanged 리스너 추가: 토글이 켜졌을 때만 패널을 활성화하도록 설정합니다.
        characterToggleButton.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                SetInventoryPanel(true);
            }
        });

        carToggleButton.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                SetInventoryPanel(false);
            }
        });

        // 닫기 버튼에 Close() 함수 연결
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => Close());
        }

        // 시작 시 캐릭터 인벤토리로 설정하고, 해당 토글을 'On' 상태로 만듭니다.
        SetInventoryPanel(true);
        characterToggleButton.isOn = true;
    }

    // 인벤토리 패널 활성화/비활성화
    private void SetInventoryPanel(bool isCharacterPanel)
    {
        // 캐릭터 인벤토리 패널 활성화
        characterInventoryPanel.SetActive(isCharacterPanel);
        // 차량 인벤토리 패널 활성화
        carInventoryPanel.SetActive(!isCharacterPanel);
    }

    // 장착 UI 업데이트 메서드 (MyRoomManager에서 호출)
    public void UpdateEquippedUI(Sprite characterSprite, string characterName, Sprite carSprite, string carName)
    {
        equippedCharacterImage.sprite = characterSprite;
        equippedCarImage.sprite = carSprite;
    }
}
