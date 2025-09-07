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

    public void Init(UnimoKartSO data, MyRoomManager manager)
    {
        this.kartData = data;
        this.myRoomManager = manager;

        kartIcon.sprite = data.kartSprite;

        if (kartNameText != null)
        {
            kartNameText.text = data.carName;
        }

        GetComponent<Button>().onClick.AddListener(OnEquipButtonClicked);

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
}
