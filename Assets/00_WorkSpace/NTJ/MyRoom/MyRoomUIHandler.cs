using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MyRoomUIHandler : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text _itemNameText;
    [SerializeField] private TMP_Text _itemLevelText;
    [SerializeField] private TMP_Text _itemDescriptionText;

    // 이 메서드를 통해 외부에서 UI를 업데이트합니다.
    public void UpdateUI(string itemName, string itemDescription, int itemLevel = 0)
    {
        _itemNameText.text = itemName;
        _itemDescriptionText.text = itemDescription;

        // 아이템에 레벨이 있을 경우에만 레벨 텍스트를 표시
        if (itemLevel > 0)
        {
            _itemLevelText.text = $"LV.{itemLevel}";
            _itemLevelText.gameObject.SetActive(true);
        }
        else
        {
            _itemLevelText.gameObject.SetActive(false);
        }
    }
}
