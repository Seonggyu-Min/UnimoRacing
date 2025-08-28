using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class TestShowFriends : MonoBehaviour
    {
        //[SerializeField] private TestInviteButtonBehaviour prefab;

        //private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;

        //private void Start()
        //{
        //    CreateFriendInviteButton();
        //}

        //private void CreateFriendInviteButton()
        //{
        //    DatabaseManager.Instance.GetOnMain(DBRoutes.FriendListRoot(CurrentUid), snap =>
        //    {
        //        if (!snap.Exists || !snap.HasChildren)
        //        {
        //            Debug.Log("친구 없음");
        //            return;
        //        }

        //        var friendUids = new List<string>();
        //        foreach (var c in snap.Children) friendUids.Add(c.Key);

        //        int left = friendUids.Count;
        //        foreach (var uid in friendUids)
        //        {
        //            DatabaseManager.Instance.GetOnMain(DBRoutes.Nickname(uid), s2 =>
        //            {
        //                string nick = s2.Value?.ToString() ?? uid;

        //                var item = Instantiate(prefab, transform);
        //                item.Init(uid, nick);
        //            },
        //            err =>
        //            {
        //                // 닉네임 실패 시 uid로 표시
        //                var item = Instantiate(prefab, transform);
        //                item.Init(uid, uid);
        //            });
        //        }
        //    });
        //}
    }
}
