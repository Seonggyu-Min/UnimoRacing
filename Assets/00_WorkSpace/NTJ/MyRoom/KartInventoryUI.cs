using DA_Assets.FCU.Model;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KartInventoryUI : MonoBehaviour
{
    private UnimoKartSO kartData;
    private MyRoomManager myRoomManager;

    [SerializeField] private Image kartIcon;
    [SerializeField] private TMP_Text kartNameText;

    // ���� ǥ�ø� ���� ����
    [SerializeField] private Image selectionOverlay;

    // �������� ���� �������� ���� ȸ�� �������� �Ǵ� ���� �����
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Button equipButton; // ��ư ���� �߰�

    public void Init(UnimoKartSO data, MyRoomManager manager, bool isOwned) // isOwned �Ű����� �߰�
    {
        this.kartData = data;
        this.myRoomManager = manager;

        kartIcon.sprite = data.kartSprite;

        if (kartNameText != null)
        {
            kartNameText.text = data.carName;
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
        myRoomManager.EquipKart(kartData);
    }

    // ���� ���¸� �����ϴ� �޼���
    public void SetSelected(bool isSelected)
    {
        if (selectionOverlay != null)
        {
            selectionOverlay.enabled = isSelected;
        }
    }

    private void UpdateVisuals(bool isOwned)
    {
        if (isOwned)
        {
            // ������ ������: ���� ��������, ��ư Ȱ��ȭ
            kartIcon.color = Color.white;
            if (lockedOverlay != null) lockedOverlay.SetActive(false);
            if (equipButton != null) equipButton.interactable = true;
        }
        else
        {
            // �������� ���� ������: ȸ������, ��ư ��Ȱ��ȭ
            kartIcon.color = Color.gray;
            if (lockedOverlay != null) lockedOverlay.SetActive(true);
            if (equipButton != null) equipButton.interactable = false;
        }
    }
}
