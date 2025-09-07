using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInventoryUI : MonoBehaviour
{
    private UnimoCharacterSO characterData;
    private MyRoomManager myRoomManager;

    [SerializeField] private Image characterIcon;
    [SerializeField] private TMP_Text characterNameText;

    // 선택 상태를 표시할 이미지 오버레이 변수 추가
    [SerializeField] private Image selectionOverlay;

    public void Init(UnimoCharacterSO data, MyRoomManager manager)
    {
        this.characterData = data;
        this.myRoomManager = manager;

        characterIcon.sprite = data.characterSprite;
        if (characterNameText != null)
        {
            characterNameText.text = data.characterName;
        }

        // 버튼 클릭 이벤트에 함수 연결
        GetComponent<Button>().onClick.AddListener(OnEquipButtonClicked);

        // 초기 상태는 선택되지 않음
        SetSelected(false);
    }

    private void OnEquipButtonClicked()
    {
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
