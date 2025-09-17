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

    // ���� ���¸� ǥ���� �̹��� �������� ���� �߰�
    [SerializeField] private Image selectionOverlay;

    // �������� ���� �������� ���� ȸ�� �������� �Ǵ� ���� �����
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Button equipButton; // ��ư ���� �߰�

    public void Init(UnimoCharacterSO data, MyRoomManager manager, bool isOwned) // isOwned �Ű����� �߰�
    {
        this.characterData = data;
        this.myRoomManager = manager;

        characterIcon.sprite = data.characterSprite;
        if (characterNameText != null)
        {
            characterNameText.text = data.characterName;
        }

        // isOwned ���¿� ���� UI ������Ʈ
        UpdateVisuals(isOwned);

        // ��ư Ŭ�� �̺�Ʈ�� �Լ� ����
        // ���� GetComponent<Button>() ��� ����ȭ�� equipButton ���
        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }

        // �ʱ� ���´� ���õ��� ����
        SetSelected(false);
    }

    private void OnEquipButtonClicked()
    {
        myRoomManager.EquipCharacter(characterData);
    }

    // �ܺο��� ȣ���Ͽ� ���� ���¸� �����ϴ� �޼���
    public void SetSelected(bool isSelected)
    {
        if (selectionOverlay != null)
        {
            // selectionOverlay �̹����� Ȱ��/��Ȱ������ ���� ó��
            selectionOverlay.enabled = isSelected;
        }
    }

    private void UpdateVisuals(bool isOwned)
    {
        if (isOwned)
        {
            // ������ ������: ���� ��������, ��ư Ȱ��ȭ
            characterIcon.color = Color.white;
            if (lockedOverlay != null) lockedOverlay.SetActive(false);
            if (equipButton != null) equipButton.interactable = true;
        }
        else
        {
            // �������� ���� ������: ȸ������, ��ư ��Ȱ��ȭ
            characterIcon.color = Color.gray;
            if (lockedOverlay != null) lockedOverlay.SetActive(true);
            if (equipButton != null) equipButton.interactable = false;
        }
    }
}
