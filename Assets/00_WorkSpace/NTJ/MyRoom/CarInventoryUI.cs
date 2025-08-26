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

    // 선택 표시를 위한 변수
    [SerializeField] private Image selectionOverlay;

    public void Init(CarData data, MyRoomManager manager)
    {
        this.carData = data;
        this.myRoomManager = manager;
        carIcon.sprite = data.carSprite;
        GetComponent<Button>().onClick.AddListener(OnEquipButtonClicked);

        // 초기 상태는 선택되지 않음
        SetSelected(false);
    }

    private void OnEquipButtonClicked()
    {
        myRoomManager.EquipCar(carData);
    }

    // 선택 상태를 변경하는 메서드
    public void SetSelected(bool isSelected)
    {
        if (selectionOverlay != null)
        {
            selectionOverlay.enabled = isSelected;
        }
    }
}
