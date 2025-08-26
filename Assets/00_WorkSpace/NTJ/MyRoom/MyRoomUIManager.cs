using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyRoomUIManager : MonoBehaviour
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

    [SerializeField] private GameObject MyRoom;

    // MyRoomManager 스크립트 참조
    private MyRoomManager myRoomManager;

    private void Start()
    {
        // MyRoomManager 스크립트를 가져와 참조합니다.
        myRoomManager = GetComponent<MyRoomManager>();

        // 버튼에 리스너 추가
        characterToggleButton.onClick.AddListener(() => SetInventoryPanel(true));
        carToggleButton.onClick.AddListener(() => SetInventoryPanel(false));

        // 시작 시 캐릭터 인벤토리로 설정
        SetInventoryPanel(true);
    }

    // 인벤토리 패널 활성화/비활성화
    public void SetInventoryPanel(bool isCharacterPanel)
    {
        // 캐릭터 인벤토리 패널 활성화
        characterInventoryPanel.SetActive(isCharacterPanel);
        // 차량 인벤토리 패널 활성화
        carInventoryPanel.SetActive(!isCharacterPanel);

        // 버튼 활성화/비활성 느낌
        // 버튼의 Image 컴포넌트 색상을 변경하거나, UI에 그레이스케일을 적용하는 등 시각적 효과를 줄 수 있습니다.
        // 여기서는 예시로 버튼의 상호작용 가능 여부를 변경합니다.
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
    public void OpenMyRoomUI()
    {
        Debug.Log("OpenMyRoomUI");
        // UI 패널을 활성화하여 화면에 표시합니다.
        MyRoom.SetActive(true);
    }
}
