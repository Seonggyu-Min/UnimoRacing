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
                () => Debug.Log("模备 夸没 己傍"),
                err => Debug.LogError($"模备 夸没 角菩: {err}")
            );
        });

        acceptButton.onClick.AddListener(() =>
        {
            string pairId = DBPathMaker.ComposePairId(myUid, friendUid);
            friendsLogic.AcceptRequest(pairId, myUid,
                () => Debug.Log("模备 夸没 荐遏 己傍"),
                err => Debug.LogError($"模备 夸没 荐遏 角菩: {err}")
            );
        });

        cancelButton.onClick.AddListener(() =>
        {
            string pairId = DBPathMaker.ComposePairId(myUid, friendUid);
            friendsLogic.CancelRequest(pairId, myUid,
                () => Debug.Log("模备 夸没 秒家 己傍"),
                err => Debug.LogError($"模备 夸没 秒家 角菩: {err}")
            );
        });
    }
}
