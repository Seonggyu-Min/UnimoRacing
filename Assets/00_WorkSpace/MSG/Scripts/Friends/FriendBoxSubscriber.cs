using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class FriendBoxSubscriber : MonoBehaviour
    {
        [SerializeField] private FriendRequestPanel _inboxPanelPrefab;
        [SerializeField] private Transform _inboxParent;

        private Dictionary<string, FriendRequestPanel> _inboxPanels = new();
        private DatabaseReference _inboxRef;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;

        private void OnEnable()
        {
            _inboxRef = FirebaseManager.Instance.Database.GetReference(DBRoutes.InBoxRoot(CurrentUid));

            _inboxRef.ValueChanged += HandleInboxChanged;
        }

        private void OnDisable()
        {
            if (_inboxRef != null)
            {
                _inboxRef.ValueChanged -= HandleInboxChanged;
                _inboxRef = null;
            }
            ClearAll();
        }

        private void HandleInboxChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"InBox 구독 오류: {args.DatabaseError.Message}");
                return;
            }

            var snap = args.Snapshot;
            Debug.Log($"[FriendBox] ValueChanged 호출됨 exists: {snap.Exists}");

            var live = new HashSet<string>();

            if (snap != null && snap.Exists)
            {
                foreach (var child in snap.Children)
                {
                    string pairId = child.Key;
                    string status = child.Child("status")?.Value?.ToString();

                    if (!string.Equals(status, DatabaseKeys.pending)) continue;

                    string[] uids = pairId.Split('_');

                    string fromUid = CurrentUid == uids[0] ? uids[1] : uids[0];
                    string toUid = CurrentUid == uids[0] ? uids[0] : uids[1];

                    if (toUid != CurrentUid)
                    {
                        Debug.LogWarning("내 인박스에 내 Uid가 아닌 요청이 있습니다. 확인 요망");
                        continue;
                    }

                    live.Add(pairId);

                    if (_inboxPanels.ContainsKey(pairId))
                        continue;

                    var panel = Instantiate(_inboxPanelPrefab, _inboxParent);
                    _inboxPanels.Add(pairId, panel);
                    panel.Init(pairId, toUid: CurrentUid, fromUid: fromUid);

                    Debug.Log($"pending 요청 패널 생성: {pairId}");
                }
            }

            RemoveDictionary(_inboxPanels, live);
        }

        private void ClearAll()
        {
            foreach (var kv in _inboxPanels)
            {
                if (kv.Value) Destroy(kv.Value.gameObject);
            }
            _inboxPanels.Clear();
        }

        private void RemoveDictionary<T>(Dictionary<string, T> dict, HashSet<string> live) where T : Component
        {
            var toRemove = new List<string>();
            foreach (var kv in dict)
            {
                if (!live.Contains(kv.Key))
                {
                    if (kv.Value)
                    {
                        Destroy(kv.Value.gameObject);
                    }
                    toRemove.Add(kv.Key);
                }
            }
            for (int i = 0; i < toRemove.Count; i++)
            {
                dict.Remove(toRemove[i]);
            }
        }
    }
}
