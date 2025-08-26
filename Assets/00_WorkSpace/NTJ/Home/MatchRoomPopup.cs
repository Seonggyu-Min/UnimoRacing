using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchRoomPopup : PopupBase
{
    [Header("방장 전용")]
    [SerializeField] private Button mapSelectButton;
    [SerializeField] private Button startGameButton;

    [Header("플레이어 전용")]
    [SerializeField] private Button readyButton;

    private void OnEnable()
    {
        readyButton.onClick.AddListener(OnReadyClicked);
        mapSelectButton.onClick.AddListener(OnMapSelectClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);

        RefreshUI();
    }

    private void OnDisable()
    {
        readyButton.onClick.RemoveListener(OnReadyClicked);
        mapSelectButton.onClick.RemoveListener(OnMapSelectClicked);
        startGameButton.onClick.RemoveListener(OnStartGameClicked);
    }

    private void RefreshUI()
    {
        bool isMaster = PhotonNetwork.IsMasterClient;

        mapSelectButton.gameObject.SetActive(isMaster);
        startGameButton.gameObject.SetActive(isMaster);
        readyButton.gameObject.SetActive(!isMaster);
    }

    private void OnReadyClicked() => MatchManager.Instance.ToggleReady();
    private void OnMapSelectClicked() => MatchManager.Instance.SelectMap();
    private void OnStartGameClicked() => MatchManager.Instance.TryStartGame();
}
