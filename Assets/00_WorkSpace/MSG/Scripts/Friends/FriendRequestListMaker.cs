using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class FriendRequestListMaker : MonoBehaviour
    {
        [SerializeField] private FriendRequestListCard _friendRequestPrefab;
        [SerializeField] private Transform _parent;

        private readonly Dictionary<string, FriendRequestListCard> _cards = new();
        private DatabaseReference _outboxRef;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;

        private void OnEnable()
        {
            _outboxRef = FirebaseManager.Instance.Database.GetReference(DBRoutes.OutBoxRoot(CurrentUid));
            _outboxRef.ValueChanged += HandleOutboxChanged;
        }

        private void OnDisable()
        {
            if (_outboxRef != null)
            {
                _outboxRef.ValueChanged -= HandleOutboxChanged;
                _outboxRef = null;
            }
            ClearAll();
        }

        private void HandleOutboxChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"[FriendRequestListMaker] Outbox 구독 오류: {args.DatabaseError.Message}");
                return;
            }

            var snap = args.Snapshot;
            var live = new HashSet<string>();

            if (snap != null && snap.Exists)
            {
                foreach (var child in snap.Children)
                {
                    string pairId = child.Key;
                    string status = child.Child(DatabaseKeys.status)?.Value?.ToString();

                    // 보낸 요청 중 pending만 필터
                    if (!string.Equals(status, DatabaseKeys.pending))
                        continue;

                    // pairId에서 상대 uid 추출
                    string[] uids = pairId.Split('_');
                    if (uids.Length != 2)
                    {
                        Debug.LogWarning($"[FriendRequestListMaker] pairId 형식이 유효하지 않습니다: {pairId}");
                        continue;
                    }
                    string otherUid = CurrentUid == uids[0] ? uids[1] : uids[0];

                    live.Add(pairId);

                    // 이미 카드 있으면 skip
                    if (_cards.ContainsKey(pairId))
                        continue;

                    // 새 카드 생성 + 초기화
                    var card = Instantiate(_friendRequestPrefab, _parent);
                    _cards.Add(pairId, card);
                    card.Init(pairId, otherUid, FriendsLogics.Instance);
                }
            }

            RemoveDictionary(_cards, live);
        }

        private void ClearAll()
        {
            foreach (var kv in _cards)
            {
                if (kv.Value) Destroy(kv.Value.gameObject);
            }
            _cards.Clear();
        }

        private void RemoveDictionary<T>(Dictionary<string, T> dict, HashSet<string> live) where T : Component
        {
            var toRemove = new List<string>();
            foreach (var kv in dict)
            {
                if (!live.Contains(kv.Key))
                {
                    if (kv.Value)
                        Destroy(kv.Value.gameObject);
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var k in toRemove)
                dict.Remove(k);
        }
    }
}
