using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace MSG
{
    public class PartyServices : MonoBehaviour
    {
        private OnDisconnect _armedPartyDisc;
        private string CurrentUid => FirebaseManager.Instance?.Auth.CurrentUser?.UserId;

        public Action<string> OnPartyJoinedChannel;
        public Action<string> OnPartyLeftChannel;
        private DatabaseReference _infoRef;
        private EventHandler<ValueChangedEventArgs> _connHandler;

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
                return s;
            return null;
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
            var infoRef = FirebaseDatabase.DefaultInstance.GetReference(".info/connected");
            _connHandler += async (s, e) =>
            {
                if (e.Snapshot?.Value is bool connected && connected)
                {
                    var partyId = await TryGetMyPartyId();
                    if (!string.IsNullOrEmpty(partyId))
                    {
                        await ArmPartyOnDisconnect(partyId);
                        // 재접속 후 파티 채널 자동 재구독
                        OnPartyJoinedChannel?.Invoke(partyId);
                    }
                }
            };
            _infoRef.ValueChanged += _connHandler;
        }



        //public void JoinParty(string partyId)
        //{
        //    Dictionary<string, object> updates = new()
        //    {
        //        { DBRoutes.PartyMember(partyId, CurrentUid), true },
        //        { DBRoutes.InPartyStatus(CurrentUid), true },
        //        { DBRoutes.PartyIdForPresence(CurrentUid), partyId },
        //        { DBRoutes.LastSeen(CurrentUid), ServerValue.Timestamp },
        //        { DBRoutes.PartyMembership(CurrentUid), partyId }
        //    };

        //    DatabaseManager.Instance.UpdateOnMain(
        //        updates,
        //        () => Debug.Log($"유저: {CurrentUid}의 파티 ID {partyId} 로 업데이트 완료"),
        //        error => Debug.LogError($"유저: {CurrentUid}의 파티 ID 업데이트 실패: {error}")
        //    );
        //}

        //public void LeaveParty(string partyId)
        //{
        //    var updates = new Dictionary<string, object>
        //    {
        //        // 파티 멤버 제거
        //        { DBRoutes.PartyMember(partyId, CurrentUid), null },

        //        // 캐시 정리
        //        { DBRoutes.InPartyStatus(CurrentUid), false },
        //        { DBRoutes.PartyIdForPresence(CurrentUid), null },
        //        { DBRoutes.LastSeen(CurrentUid), ServerValue.Timestamp },
        //        { DBRoutes.PartyMembership(CurrentUid), null }
        //    };

        //    DatabaseManager.Instance.UpdateOnMain(
        //        updates,
        //        () => Debug.Log($"유저: {CurrentUid}의 파티 나가기 완료"),
        //        error => Debug.LogError($"유저: {CurrentUid}의 파티 나가기 실패: {error}")
        //    );
        //}
    }
}
