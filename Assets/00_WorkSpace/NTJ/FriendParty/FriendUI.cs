using MSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendUI : MonoBehaviour
{
    [SerializeField] private Button sendButton;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private string myUid;
    [SerializeField] private string friendUid;

    private FriendsLogics friendsLogic;

    void Start()
    {
        friendsLogic = FindObjectOfType<FriendsLogics>();

        sendButton.onClick.AddListener(() =>
        {
            friendsLogic.SendRequest(myUid, friendUid,
                () => Debug.Log("ģ�� ��û ����"),
                err => Debug.LogError($"ģ�� ��û ����: {err}")
            );
        });

        acceptButton.onClick.AddListener(() =>
        {
            string pairId = DBPathMaker.ComposePairId(myUid, friendUid);
            friendsLogic.AcceptRequest(pairId, myUid,
                () => Debug.Log("ģ�� ��û ���� ����"),
                err => Debug.LogError($"ģ�� ��û ���� ����: {err}")
            );
        });

        cancelButton.onClick.AddListener(() =>
        {
            string pairId = DBPathMaker.ComposePairId(myUid, friendUid);
            friendsLogic.CancelRequest(pairId, myUid,
                () => Debug.Log("ģ�� ��û ��� ����"),
                err => Debug.LogError($"ģ�� ��û ��� ����: {err}")
            );
        });
    }
}
