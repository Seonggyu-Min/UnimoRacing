using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeUI : MonoBehaviour
{
    [Header("팝업 버튼")]
    [SerializeField] private Button shopButton; // 상점 버튼
    [SerializeField] private PopupBase shopPopup;

    [SerializeField] private Button myRoomButton; // 마이룸 버튼
    [SerializeField] private PopupBase myRoomPopup;

    [SerializeField] private Button missionButton; // 미션 버튼
    [SerializeField] private PopupBase missionPopup;

    [SerializeField] private Button settingsButton; // 설정 버튼
    [SerializeField] private PopupBase settingsPopup;

    [SerializeField] private Button removeAdButton; // 광고 제거 버튼
    [SerializeField] private PopupBase removeAdPopup;

    [SerializeField] private Button specialGiftButton; // 특별 선물 버튼
    [SerializeField] private PopupBase specialGiftPopup;

    [SerializeField] private Button friendsButton; // 친구 버튼
    [SerializeField] private PopupBase friendsPopup;

    [SerializeField] private Button matchButton; // 매치 버튼
    [SerializeField] private MatchPopup matchPopup;


    [Header("씬 이동 버튼들")]
    [SerializeField] private Button playButton;  // 게임 시작 버튼
    [SerializeField] private string gameSceneName = "GameScene";


    private void Start()
    {
        // 팝업 열기 연결
        shopButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(shopPopup));
        myRoomButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(myRoomPopup));
        missionButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(missionPopup));
        settingsButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(settingsPopup));
        removeAdButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(removeAdPopup));
        specialGiftButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(specialGiftPopup));
        friendsButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(friendsPopup));
        matchButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(matchPopup));

        // 씬 이동 연결
        playButton.onClick.AddListener(() => UIManager.Instance.LoadScene(gameSceneName));
    }
}
