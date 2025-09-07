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

    public void Init(UnimoCharacterSO data, MyRoomManager manager)
    {
        this.characterData = data;
        this.myRoomManager = manager;

        characterIcon.sprite = data.characterSprite;
        if (characterNameText != null)
        {
            characterNameText.text = data.characterName;
        }

        // ��ư Ŭ�� �̺�Ʈ�� �Լ� ����
        GetComponent<Button>().onClick.AddListener(OnEquipButtonClicked);

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
}
