using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInventoryUI : MonoBehaviour
{
    private CharacterData characterData;
    private MyRoomManager myRoomManager;

    [SerializeField] private Image characterIcon;
    [SerializeField] private TMP_Text characterNameText;

    // 선택 상태를 표시할 이미지 오버레이 변수 추가
    [SerializeField] private Image selectionOverlay;

    public void Init(CharacterData data, MyRoomManager manager)
    {
        this.characterData = data;
        this.myRoomManager = manager;

        // characterSprite를 가져올 때, data.CharacterSprite 속성 사용
        characterIcon.sprite = data.characterSprite;
        // characterName을 가져올 때, data.CharacterName 속성 사용
        // if (characterNameText != null)
        // {
        //     characterNameText.text = data.characterName;
        // }

        // 버튼 클릭 이벤트에 함수 연결
        GetComponent<Button>().onClick.AddListener(OnEquipButtonClicked);

        // 초기 상태는 선택되지 않음
        SetSelected(false);
    }

    private void OnEquipButtonClicked()
    {
        // myRoomManager의 EquipCharacter 메서드 호출
        myRoomManager.EquipCharacter(characterData);
    }

    // 외부에서 호출하여 선택 상태를 변경하는 메서드
    public void SetSelected(bool isSelected)
    {
        if (selectionOverlay != null)
        {
            // selectionOverlay 이미지의 활성/비활성으로 음영 처리
            selectionOverlay.enabled = isSelected;
        }
    }
}
