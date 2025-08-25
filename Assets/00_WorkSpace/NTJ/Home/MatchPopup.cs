using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchPopup : PopupBase
{
    [Header("매칭 관련 버튼")]
    [SerializeField] private GameObject playButtonGroup;     // Play! 버튼 그룹
    [SerializeField] private GameObject matchingButtonGroup; // 매칭중... 버튼 그룹

    [SerializeField] private Button startMatchButton;   // Play! 버튼
    [SerializeField] private Button cancelMatchButton;  // 매칭중일 때 취소 버튼

    private void OnEnable()
    {
        startMatchButton.onClick.AddListener(OnStartMatch);
        cancelMatchButton.onClick.AddListener(OnCancelMatch);

        RefreshUI(false); // 기본은 Play 상태
    }

    private void OnDisable()
    {
        startMatchButton.onClick.RemoveListener(OnStartMatch);
        cancelMatchButton.onClick.RemoveListener(OnCancelMatch);
    }

    private void OnStartMatch()
    {
        MatchManager.Instance.StartMatch();
        RefreshUI(true); // 매칭중 상태로 전환
    }

    private void OnCancelMatch()
    {
        MatchManager.Instance.CancelMatch();
        UIManager.Instance.ClosePopup();
        RefreshUI(false); // 다시 Play 상태
    }

    private void RefreshUI(bool isMatching)
    {
        playButtonGroup.SetActive(!isMatching);
        matchingButtonGroup.SetActive(isMatching);
    }
}
