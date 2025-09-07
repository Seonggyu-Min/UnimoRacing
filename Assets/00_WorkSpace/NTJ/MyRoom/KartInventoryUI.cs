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

    // 선택 표시를 위한 변수
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

        // 초기 상태는 선택되지 않음
        SetSelected(false);
    }

    private void OnEquipButtonClicked()
    {
        myRoomManager.EquipKart(kartData);
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
