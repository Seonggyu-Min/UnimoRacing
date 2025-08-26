using MSG;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendsUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private Button sendRequestButton;
    [SerializeField] private TMP_Text logText;
    [SerializeField] private Transform friendListParent;
    [SerializeField] private GameObject friendListPrefab;

    [Header("Managers")]
    [SerializeField] private PartyManager partyManager;

    [Header("Car Icons")]
    [SerializeField] private Sprite car1Icon;
    [SerializeField] private Sprite car2Icon;
    [SerializeField] private Sprite car3Icon;

    private FriendsLogics friendsLogic;
    private string myUid = "myUid_123";
    private string targetUid = "";

    private List<(string uid, string name, int level, Sprite carIcon)> dummyFriends;

    void Start()
    {
        friendsLogic = FindObjectOfType<FriendsLogics>();

        // 샘플 차량 데이터
        dummyFriends = new List<(string, string, int, Sprite)>()
        {
            ("uid_001", "Primo", 1, car1Icon),
            ("uid_002", "Highfish", 2, car2Icon),
            ("uid_003", "Sofo", 3, car3Icon)
        };

        // UID 검색 입력 감지
        searchInputField.onValueChanged.AddListener(value => targetUid = value);

        // 친구 요청 버튼
        sendRequestButton.onClick.AddListener(() =>
        {
            if (string.IsNullOrEmpty(targetUid))
            {
                LogMessage("친구 UID를 입력하세요!");
                return;
            }

            friendsLogic.SendRequest(myUid, targetUid,
                () => LogMessage("친구 요청 성공"),
                err => LogMessage($"친구 요청 실패: {err}")
            );
        });

        PopulateFriendList();
    }

    private void PopulateFriendList()
    {
        // 기존 리스트 초기화
        foreach (Transform child in friendListParent)
            Destroy(child.gameObject);

        // 새 친구 리스트 생성
        foreach (var friend in dummyFriends)
        {
            GameObject item = Instantiate(friendListPrefab, friendListParent);
            var ui = item.GetComponent<FriendUI>();
            ui.Init(friend.uid, friend.name, friend.level, friend.carIcon, partyManager);
        }
    }

    private void LogMessage(string message)
    {
        if (logText != null)
            logText.text = message;
        Debug.Log(message);
    }
}
