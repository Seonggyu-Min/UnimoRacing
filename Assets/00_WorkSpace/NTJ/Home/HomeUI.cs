using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeUI : MonoBehaviour
{
    [Header("�˾� ��ư")]
    [SerializeField] private Button shopButton; // ���� ��ư
    [SerializeField] private PopupBase shopPopup;

    [SerializeField] private Button myRoomButton; // ���̷� ��ư
    [SerializeField] private PopupBase myRoomPopup;

    [SerializeField] private Button missionButton; // �̼� ��ư
    [SerializeField] private PopupBase missionPopup;

    [SerializeField] private Button settingsButton; // ���� ��ư
    [SerializeField] private PopupBase settingsPopup;

    [SerializeField] private Button removeAdButton; // ���� ���� ��ư
    [SerializeField] private PopupBase removeAdPopup;

    [SerializeField] private Button specialGiftButton; // Ư�� ���� ��ư
    [SerializeField] private PopupBase specialGiftPopup;

    [SerializeField] private Button friendsButton; // ģ�� ��ư
    [SerializeField] private PopupBase friendsPopup;

    [SerializeField] private Button matchButton; // ��ġ ��ư
    [SerializeField] private MatchPopup matchPopup;


    [Header("�� �̵� ��ư��")]
    [SerializeField] private Button playButton;  // ���� ���� ��ư
    [SerializeField] private string gameSceneName = "GameScene";


    private void Start()
    {
        // �˾� ���� ����
        shopButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(shopPopup));
        myRoomButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(myRoomPopup));
        missionButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(missionPopup));
        settingsButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(settingsPopup));
        removeAdButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(removeAdPopup));
        specialGiftButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(specialGiftPopup));
        friendsButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(friendsPopup));
        matchButton.onClick.AddListener(() => UIManager.Instance.OpenPopup(matchPopup));

        // �� �̵� ����
        playButton.onClick.AddListener(() => UIManager.Instance.LoadScene(gameSceneName));
    }
}
