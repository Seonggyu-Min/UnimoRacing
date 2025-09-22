using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemSelectButton : MonoBehaviour
{
    // 인스펙터에서 MyRoomUIHandler 스크립트가 붙은 오브젝트를 연결합니다.
    [SerializeField] private MyRoomUIHandler _myRoomUIHandler;

    // 이 아이템의 데이터를 미리 설정
    [Header("Item Data")]
    [SerializeField] private string _itemName;
    [SerializeField] private string _itemDescription;
    [SerializeField] private int _itemLevel = 0; // 레벨이 없는 아이템의 경우 0

    private Button _button;

    private void Start()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(OnItemClick);
        }
    }

    private void OnItemClick()
    {
        // 클릭 시 MyRoomUIHandler에 데이터 전달하여 UI 업데이트
        if (_myRoomUIHandler != null)
        {
            _myRoomUIHandler.UpdateUI(_itemName, _itemDescription, _itemLevel);
        }
    }
}
