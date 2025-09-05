using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendListToggler : MonoBehaviour
{
    // �ν����Ϳ��� ģ�� ��� �г��� �����մϴ�.
    [SerializeField] private GameObject friendListPanel;
    // �ν����Ϳ��� ģ�� ��û ��� �г��� �����մϴ�.
    [SerializeField] private GameObject friendRequestPanel;
    // �ν����Ϳ��� ��� ��ư�� �����մϴ�.
    [SerializeField] private Button toggleButton;
    // �ν����Ϳ��� ��� ��ư�� �ؽ�Ʈ�� �����մϴ�.
    [SerializeField] private TMP_Text toggleButtonText;

    // ���� ģ�� ��� ȭ���� ���̴��� ���θ� �����մϴ�.
    private bool _isFriendListVisible = true;

    private void Awake()
    {
        // ��� ��ư�� Ŭ�� �̺�Ʈ�� �����մϴ�.
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(OnToggleButtonClick);
        }

        // �ʱ� ���¸� �����մϴ�.
        InitializePanels();
    }

    private void InitializePanels()
    {
        // ó������ ģ�� ��� �гθ� ���̰� �����մϴ�.
        if (friendListPanel != null)
        {
            friendListPanel.SetActive(true);
        }
        if (friendRequestPanel != null)
        {
            friendRequestPanel.SetActive(false);
        }

        // ��ư �ؽ�Ʈ�� "ģ�� ��û ���"���� �ʱ�ȭ�մϴ�.
        if (toggleButtonText != null)
        {
            toggleButtonText.text = "ģ�� ��û ���";
        }
    }

    private void OnToggleButtonClick()
    {
        // ���¸� ������ŵ�ϴ�.
        _isFriendListVisible = !_isFriendListVisible;

        // ���¿� ���� �г��� Ȱ��ȭ/��Ȱ��ȭ�մϴ�.
        if (friendListPanel != null)
        {
            friendListPanel.SetActive(_isFriendListVisible);
        }
        if (friendRequestPanel != null)
        {
            friendRequestPanel.SetActive(!_isFriendListVisible);
        }

        // ���¿� ���� ��ư �ؽ�Ʈ�� �����մϴ�.
        if (toggleButtonText != null)
        {
            toggleButtonText.text = _isFriendListVisible ? "ģ�� ��û ���" : "ģ�� ���";
        }
    }
}
