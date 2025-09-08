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

    // 구매하지 않은 아이템을 위한 회색 오버레이 또는 색상 변경용
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Button equipButton; // 버튼 변수 추가

    public void Init(UnimoCharacterSO data, MyRoomManager manager, bool isOwned) // isOwned 매개변수 추가
    {
        this.characterData = data;
        this.myRoomManager = manager;

        characterIcon.sprite = data.characterSprite;
        if (characterNameText != null)
        {
            characterNameText.text = data.characterName;
        }

        // isOwned 상태에 따라 UI 업데이트
        UpdateVisuals(isOwned);

        // 버튼 클릭 이벤트에 함수 연결
        // 기존 GetComponent<Button>() 대신 직렬화된 equipButton 사용
        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }

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

    private void UpdateVisuals(bool isOwned)
    {
        if (isOwned)
        {
            // 소유한 아이템: 원래 색상으로, 버튼 활성화
            characterIcon.color = Color.white;
            if (lockedOverlay != null) lockedOverlay.SetActive(false);
            if (equipButton != null) equipButton.interactable = true;
        }
        else
        {
            // 소유하지 않은 아이템: 회색으로, 버튼 비활성화
            characterIcon.color = Color.gray;
            if (lockedOverlay != null) lockedOverlay.SetActive(true);
            if (equipButton != null) equipButton.interactable = false;
        }
    }
}
