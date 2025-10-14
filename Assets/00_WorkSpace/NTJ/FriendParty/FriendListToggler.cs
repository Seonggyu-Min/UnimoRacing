using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendListToggler : MonoBehaviour
{
    // 인스펙터에서 친구 목록 패널을 연결합니다.
    [SerializeField] private GameObject friendListPanel;
    // 인스펙터에서 친구 요청 목록 패널을 연결합니다.
    [SerializeField] private GameObject friendRequestPanel;
    // 인스펙터에서 토글 버튼을 연결합니다.
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button toggleButton2;

    // 현재 친구 목록 화면이 보이는지 여부를 추적합니다.
    private bool _isFriendListVisible = true;

    private void Awake()
    {
        // 토글 버튼에 클릭 이벤트를 연결합니다.
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(OnToggleButtonClick);
        }

        if (toggleButton2 != null)
        {
            toggleButton2.onClick.AddListener(OnToggleButtonClick);
        }

        // 초기 상태를 설정합니다.
        InitializePanels();
    }

    private void InitializePanels()
    {
        // 처음에는 친구 목록 패널만 보이게 설정합니다.
        if (friendListPanel != null)
        {
            friendListPanel.SetActive(true);
        }
        if (friendRequestPanel != null)
        {
            friendRequestPanel.SetActive(false);
        }
    }

    private void OnToggleButtonClick()
    {
        // 상태를 반전시킵니다.
        _isFriendListVisible = !_isFriendListVisible;

        // 상태에 따라 패널을 활성화/비활성화합니다.
        if (friendListPanel != null)
        {
            friendListPanel.SetActive(_isFriendListVisible);
        }
        if (friendRequestPanel != null)
        {
            friendRequestPanel.SetActive(!_isFriendListVisible);
        }
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(OnToggleButtonClick);
        }

        if (toggleButton2 != null)
        {
            toggleButton2.onClick.RemoveListener(OnToggleButtonClick);
        }
    }
}
