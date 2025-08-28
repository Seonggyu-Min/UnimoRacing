using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSG
{
    public class MatchFlowManager : MonoBehaviour
    {
        [SerializeField] private PartyService _party;
        [SerializeField] private RoomAgent _room;
        [SerializeField] private ChatDM _chat;
        [SerializeField] private LeaderMatchFinder _leaderMatch;
        [SerializeField] private FollowerMatchHandler _followerMatch;

        private string _myUid;

        private async void Start()
        {
            _myUid = FirebaseManager.Instance.Auth.CurrentUser.UserId;
            _chat.Initialize(_myUid);

            _chat.OnDirectMessageReceived += OnDm;
            _party.OnPartyChanged += OnPartyChanged;
            _party.SetSolo();

            string home = GetCurrentHome();
            await _room.EnsureHomeRoomAsync(home);
        }

        private async void OnPartyChanged()
        {
            string home = GetCurrentHome();
            await _room.EnsureHomeRoomAsync(home);
        }

        private void OnDestroy()
        {
            _chat.OnDirectMessageReceived -= OnDm;
            _party.OnPartyChanged -= OnPartyChanged;
        }

        private string GetCurrentHome()
        {
            if (_party.IsInParty)
            {
                return RoomMakeHelper.PartyHome(_party.LeaderUid);
            }
            return RoomMakeHelper.Personal(_myUid);
        }


        public async void OnClickQuickMatch()
        {
            // 파티가 아닌 경우에는 바로 퀵매치
            if (!_party.IsInParty)
            {
                await _leaderMatch.StartQuickMatchAsync(1); // 랜덤방 참여 시도 후 실패 시 후보방 생성
                return;
            }

            // 파티인 경우는 리더만 시작
            if (!_party.IsLeader)
            {
                Debug.Log("리더만 시작 가능");
                return;
            }

            int partySize = _party.Members.Count;
            await _leaderMatch.StartQuickMatchAsync(partySize);
        }

        public void OnClickCancelMatch()
        {
            if (_party.IsInParty && _party.IsLeader)
            {
                _leaderMatch.CancelMatch();
            }
        }


        private async void OnDm(string senderUid, DMType type, string payload)
        {
            if (_party.IsInParty && _party.IsLeader) return; // 리더는 DM받을 필요 없음

            switch (type)
            {
                case DMType.Invite:
                    await _followerMatch.JoinInviteAsync(payload);
                    break;
                case DMType.Out:
                    await _followerMatch.LeaveNowAsync();
                    break;
                case DMType.Recall:
                    await _followerMatch.ReturnHomeAsync(payload);
                    break;
                case DMType.Cancel:
                    await _followerMatch.ReturnHomeAsync(GetCurrentHome());
                    break;
            }
        }
    }
}
