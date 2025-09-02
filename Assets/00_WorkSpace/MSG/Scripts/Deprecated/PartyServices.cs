using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace MSG.Deprecated
{
    public class PartyServices : MonoBehaviour
    {
        private string _currentPartyId;
        private OnDisconnect _armedPartyDisc;
        private DatabaseReference _infoRef;
        private EventHandler<ValueChangedEventArgs> _connHandler;

        private DatabaseReference _leaderRef;
        private EventHandler<ValueChangedEventArgs> _leaderHandler;
        private string _leaderUidCached;

        private DatabaseReference _membersRef;
        private EventHandler<ValueChangedEventArgs> _membersHandler;
        private HashSet<string> _members = new();

        public event Action<string> OnPartyJoinedChannel;
        public event Action<string> OnPartyLeftChannel;
        public event Action<string> OnLeaderChanged;

        private string CurrentUid => FirebaseManager.Instance?.Auth.CurrentUser?.UserId;
        public string CurrentPartyId => _currentPartyId; // null이면 솔로로 간주
        public HashSet<string> Members => _members;
        public string LeaderUid => _leaderUidCached;
        public bool IsLeader
        {
            get
            {
                var pid = _currentPartyId;
                if (IsSoloPartyId(pid))
                {
                    return string.Equals(SoloOwnerUid(pid) ?? CurrentUid, CurrentUid, StringComparison.Ordinal);
                }
                return string.Equals(_leaderUidCached, CurrentUid, StringComparison.Ordinal);
            }
        }


        private void OnEnable()
        {
            WatchConnection();
        }

        private void OnDisable()
        {
            if (_infoRef != null && _connHandler != null)
            {
                _infoRef.ValueChanged -= _connHandler;
            }
            _infoRef = null;
            _connHandler = null;
        }


        // 파티 들어가기
        public async void JoinParty(string partyId)
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentUid) || string.IsNullOrEmpty(partyId))
                {
                    Debug.LogWarning("[Party] JoinParty invalid args");
                    return;
                }

                // 기존 소속 확인
                var prevPartyId = await TryGetMyPartyId();

                if (!string.IsNullOrEmpty(prevPartyId) && prevPartyId != partyId)
                {
                    // 기존 파티 탈퇴 및 예약 해제 포함
                    await LeavePartyInternal(prevPartyId, cancelOnDisconnect: true);
                }

                // 가입 내역 업데이트
                var updates = new Dictionary<string, object>
                {
                    { DBRoutes.PartyMember(partyId, CurrentUid), true },
                    { DBRoutes.InPartyStatus(CurrentUid), true },
                    { DBRoutes.PartyIdForPresence(CurrentUid), partyId },
                    { DBRoutes.PartyMembership(CurrentUid), partyId },
                    { DBRoutes.LastSeen(CurrentUid), ServerValue.Timestamp },
                };

                await FirebaseManager.Instance.Database.RootReference
                    .UpdateChildrenAsync(updates);

                Debug.Log($"[Party] Joined party: {partyId}");
                _currentPartyId = partyId;
                StartWatchParty(partyId);

                // 끊기면 자동 탈퇴 예약
                await ArmPartyOnDisconnect(partyId);

                // 파티 채널 구독
                OnPartyJoinedChannel?.Invoke(partyId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Party] JoinParty failed: {e}");
            }
        }

        // 파티 나가기
        public async void LeaveParty(string partyId)
        {
            try
            {
                await LeavePartyInternal(partyId, cancelOnDisconnect: true);
                _currentPartyId = string.Empty;
                StopWatchParty();
                Debug.Log($"[Party] Left party: {partyId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Party] LeaveParty failed: {e}");
            }
        }

        // 현재 소속 파티ID 조회
        public async Task<string> TryGetMyPartyId()
        {
            var snap = await FirebaseManager.Instance.Database
                .GetReference(DBRoutes.PartyMembership(CurrentUid))
                .GetValueAsync();

            if (snap.Exists && snap.Value is string s && !string.IsNullOrEmpty(s))
            {
                return s;
            }
            return null;
        }

        private void StartWatchParty(string partyId)
        {
            StopWatchParty();

            if (IsSoloPartyId(partyId))
            {
                _leaderUidCached = SoloOwnerUid(partyId) ?? CurrentUid;
                _members.Clear();
                _members.Add(CurrentUid);
                OnLeaderChanged?.Invoke(_leaderUidCached);
                return;
            }

            _leaderRef = FirebaseManager.Instance.Database.GetReference(DBRoutes.PartyLeader(partyId));
            _leaderHandler = (s, e) =>
            {
                var newLeader = e.Snapshot?.Value as string;
                if (!string.Equals(_leaderUidCached, newLeader, StringComparison.Ordinal))
                {
                    _leaderUidCached = newLeader;
                    OnLeaderChanged?.Invoke(newLeader);
                    Debug.Log($"[Party] 리더 변경됨: {newLeader}");
                }
            };
            _leaderRef.ValueChanged += _leaderHandler;

            _membersRef = FirebaseManager.Instance.Database.GetReference(DBRoutes.PartyMembers(partyId));
            _membersHandler = (s, e) =>
            {
                _members.Clear();
                if (e.Snapshot != null && e.Snapshot.Exists)
                {
                    foreach (var child in e.Snapshot.Children)
                    {
                        if (child.Value is bool b && b == true)
                        {
                            _members.Add(child.Key);
                        }
                    }
                }
            };
            _membersRef.ValueChanged += _membersHandler;
        }

        private void StopWatchParty()
        {
            if (_leaderRef != null && _leaderHandler != null)
            {
                _leaderRef.ValueChanged -= _leaderHandler;
            }
            _leaderRef = null;
            _leaderHandler = null;

            if (_membersRef != null && _membersHandler != null)
            {
                _membersRef.ValueChanged -= _membersHandler;
            }
            _membersRef = null;
            _membersHandler = null;

            _leaderUidCached = null;
            _members.Clear();
        }

        private async Task LeavePartyInternal(string partyId, bool cancelOnDisconnect)
        {
            if (string.IsNullOrEmpty(partyId)) return;

            // OnDisconnect 예약 해제
            if (cancelOnDisconnect) await CancelPartyOnDisconnect();

            // DB 멀티 업데이트
            var updates = new Dictionary<string, object>
            {
                { DBRoutes.PartyMember(partyId, CurrentUid), null },
                { DBRoutes.InPartyStatus(CurrentUid), false },
                { DBRoutes.PartyIdForPresence(CurrentUid), null },
                { DBRoutes.PartyMembership(CurrentUid), null },
                { DBRoutes.LastSeen(CurrentUid), ServerValue.Timestamp },
            };

            await FirebaseManager.Instance.Database.RootReference
                .UpdateChildrenAsync(updates);

            // 채널 해제
            OnPartyLeftChannel?.Invoke(partyId);
        }

        // 파티용 OnDisconnect 예약: 연결이 끊기면 자동 탈퇴되도록 설정
        private async Task ArmPartyOnDisconnect(string partyId)
        {
            var updates = new Dictionary<string, object>
            {
                { DBRoutes.PartyMember(partyId, CurrentUid), null },
                { DBRoutes.PartyMembership(CurrentUid), null },
                { DBRoutes.InPartyStatus(CurrentUid), false },
                { DBRoutes.PartyIdForPresence(CurrentUid), null },
                { DBRoutes.LastSeen(CurrentUid), ServerValue.Timestamp },
            };

            var onDisc = FirebaseManager.Instance.Database.RootReference.OnDisconnect();
            await onDisc.UpdateChildren(updates);
            _armedPartyDisc = onDisc;

            Debug.Log($"[Party] Armed OnDisconnect for party {partyId}");
        }

        private async Task CancelPartyOnDisconnect()
        {
            if (_armedPartyDisc != null)
            {
                await _armedPartyDisc.Cancel();
                _armedPartyDisc = null;
                Debug.Log("[Party] Canceled OnDisconnect");
            }
        }

        // 재연결 시점에 현재 소속이 있다면 OnDisconnect 재설정
        private void WatchConnection()
        {
            _infoRef = FirebaseDatabase.DefaultInstance.GetReference(".info/connected");
            _connHandler += async (s, e) =>
            {
                if (e.Snapshot?.Value is bool connected && connected)
                {
                    var partyId = await TryGetMyPartyId();
                    if (!string.IsNullOrEmpty(partyId))
                    {
                        _currentPartyId = partyId;
                        StartWatchParty(partyId); // 파티 다시 Watch
                        await ArmPartyOnDisconnect(partyId);
                        OnPartyJoinedChannel?.Invoke(partyId); // 재접속 후 파티 채널 자동 재구독
                    }
                }
            };
            _infoRef.ValueChanged += _connHandler;
        }

        private bool IsSoloPartyId(string partyId)
        {
            return string.IsNullOrEmpty(partyId) || partyId.StartsWith("solo:", StringComparison.Ordinal);
        }
        private string SoloOwnerUid(string partyId)
        {
            return IsSoloPartyId(partyId) && partyId?.Length > 5 ? partyId.Substring(5) : null;
        }

        //public async Task<bool> AmIPartyLeader(string partyId = null)
        //{
        //    var uid = CurrentUid;
        //    if (string.IsNullOrEmpty(uid)) return false;

        //    partyId ??= _currentPartyId;

        //    // 솔로는 무조건 본인이 리더
        //    if (IsSoloPartyId(partyId))
        //    {
        //        var soloOwner = SoloOwnerUid(partyId);
        //        return string.IsNullOrEmpty(soloOwner) || string.Equals(soloOwner, uid, StringComparison.Ordinal);
        //    }

        //    // 리더 경로 조회
        //    var leaderRef = FirebaseManager.Instance.Database.GetReference(DBRoutes.PartyLeader(partyId));
        //    var snap = await leaderRef.GetValueAsync();
        //    var leader = (snap.Exists ? snap.Value as string : null) ?? _leaderUidCached;

        //    return string.Equals(leader, uid, StringComparison.Ordinal);
        //}
    }
}
