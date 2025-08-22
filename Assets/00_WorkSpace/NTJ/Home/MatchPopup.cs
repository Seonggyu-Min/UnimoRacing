using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchPopup : PopupBase
{
    [Header("��Ī ���� ��ư")]
    [SerializeField] private GameObject playButtonGroup;     // Play! ��ư �׷�
    [SerializeField] private GameObject matchingButtonGroup; // ��Ī��... ��ư �׷�

    [SerializeField] private Button startMatchButton;   // Play! ��ư
    [SerializeField] private Button cancelMatchButton;  // ��Ī���� �� ��� ��ư

    private void OnEnable()
    {
        startMatchButton.onClick.AddListener(OnStartMatch);
        cancelMatchButton.onClick.AddListener(OnCancelMatch);

        RefreshUI(false); // �⺻�� Play ����
    }

    private void OnDisable()
    {
        startMatchButton.onClick.RemoveListener(OnStartMatch);
        cancelMatchButton.onClick.RemoveListener(OnCancelMatch);
    }

    private void OnStartMatch()
    {
        MatchManager.Instance.StartMatch();
        RefreshUI(true); // ��Ī�� ���·� ��ȯ
    }

    private void OnCancelMatch()
    {
        MatchManager.Instance.CancelMatch();
        UIManager.Instance.ClosePopup();
        RefreshUI(false); // �ٽ� Play ����
    }

    private void RefreshUI(bool isMatching)
    {
        playButtonGroup.SetActive(!isMatching);
        matchingButtonGroup.SetActive(isMatching);
    }
}
