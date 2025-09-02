using DA_Assets.FCU.Model;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarInventoryUI : MonoBehaviour
{
    private CarData carData;
    private MyRoomManager myRoomManager;
    [SerializeField] private Image carIcon;
    [SerializeField] private TMP_Text carNameText;

    // ���� ǥ�ø� ���� ����
    [SerializeField] private Image selectionOverlay;

    public void Init(CarData data, MyRoomManager manager)
    {
        this.carData = data;
        this.myRoomManager = manager;
        carIcon.sprite = data.carSprite;
        // carName�� ������ ��, data.CarName �Ӽ� ���
        if (carNameText != null)
        {
            carNameText.text = data.carName;
        }
        GetComponent<Button>().onClick.AddListener(OnEquipButtonClicked);

        // �ʱ� ���´� ���õ��� ����
        SetSelected(false);
    }

    private void OnEquipButtonClicked()
    {
        myRoomManager.EquipCar(carData);
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
