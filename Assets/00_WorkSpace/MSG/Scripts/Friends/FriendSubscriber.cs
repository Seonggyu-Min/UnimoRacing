using Firebase.Database;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class FriendSubscriber : MonoBehaviour
    {
        [SerializeField] private ChatDM _chatDM;

        [SerializeField] private PartyRequestCard _partyRequestCard;
        [SerializeField] private Transform _parent;

        private Action _unsubscribe;
        private List<GameObject> _cardList = new();
        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;


        private void Start()
        {
            Subscribe();
        }


        private void OnDisable()
        {
            Unsubscribe();
        }


        private void Subscribe()
        {
            Unsubscribe();

            _unsubscribe = DatabaseManager.Instance.SubscribeValueChanged(
                DBRoutes.FriendListRoot(CurrentUid),
                OnFriendListChanged,
                err => Debug.LogError($"[Friends] Subscribe error: {err}")
            );
        }

        private void Unsubscribe()
        {
            _unsubscribe?.Invoke();
            _unsubscribe = null;
        }

        private void OnFriendListChanged(DataSnapshot snap)
        {
            List<string> friendUids = new();

            if (snap != null && snap.Exists)
            {
                foreach (var child in snap.Children)
                {
                    string friendUid = child.Key;
                    bool isFriend = false;

                    if (child.Value != null)
                    {
                        // true 저장을 전제
                        bool.TryParse(child.Value.ToString(), out isFriend);
                    }

                    if (isFriend)
                    {
                        friendUids.Add(friendUid);
                    }
                }
            }

            RemakeFriendUI(friendUids);
        }


        // 임시로 모두 생성과 삭제
        private void RemakeFriendUI(List<string> friendUids)
        {
            foreach (var card in _cardList) Destroy(card);
            _cardList.Clear();

            foreach (var uid in friendUids)
            {
                PartyRequestCard card = Instantiate(_partyRequestCard, _parent);
                card.Init(uid, _chatDM);
                _cardList.Add(card.gameObject);
            }
        }
    }
}
