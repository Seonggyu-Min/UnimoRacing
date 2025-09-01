using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        [SerializeField] private GameStarter _gameStarter;

        [SerializeField] private PartyJoinPanel _partyJoinPanel;

        private bool _initialized = false;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;


        private void Start()
        {
            _chat.Initialize(CurrentUid);

            _chat.OnDirectMessageReceived += OnDm;
            _party.OnPartyChanged += OnPartyChanged;

            if (!_initialized)
            {
                _party.SetSolo();
                _initialized = true;
            }
        }

        private async void OnPartyChanged()
        {
            if (_gameStarter.IsStarted)
            {
                Debug.LogWarning("[OnPartyChanged] 게임 중에 파티가 변경되어 return");
                return;
            }
            string home = GetCurrentHome();

            Debug.Log($"[OnPartyChanged] IsInParty: {_party.IsInParty}, Leader: {_party.LeaderUid}, home: {home}");

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
            return RoomMakeHelper.Personal(CurrentUid);
        }


        public async void OnClickQuickMatch()
        {
            // 파티가 아닌 경우에는 바로 퀵매치
            if (!_party.IsInParty)
            {
                Debug.Log("솔로로 매치 시작");
                await _leaderMatch.StartQuickMatchAsync(0); // 랜덤방 참여 시도 후 실패 시 후보방 생성
                return;
            }

            // 파티인 경우는 리더만 시작
            if (!_party.IsLeader)
            {
                Debug.Log("리더만 시작 가능");
                return;
            }

            int partySize = _party.Members.Count;
            await _leaderMatch.StartQuickMatchAsync(partySize - 1);
        }

        public async void OnClickCancelMatch()
        {
            if (_party.IsInParty && _party.IsLeader)
            {
                _leaderMatch.CancelMatch();
            }
            else if (!_party.IsInParty)
            {
                await _followerMatch.ReturnHomeAsync(RoomMakeHelper.Personal(CurrentUid));
            }
        }

        public void OnClickLeaveParty()
        {
            if (!_party.IsInParty)
            {
                Debug.Log("파티에 있지 않습니다");
                return;
            }

            // 리더는 파티 탈퇴 시 해산
            if (_party.IsLeader)
            {
                // 모든 멤버에게 해산 통지
                foreach (var uid in _party.Members)
                {
                    if (uid == _party.LeaderUid) continue;
                    _chat.SendPartyDisband(uid, _party.LeaderUid);
                }

                _party.SetSolo(); // 리더 자신도 나가기
            }
            // 팔로워는 파티 탈퇴 시 혼자만 나감
            else
            {
                _chat.SendPartyLeave(_party.LeaderUid, CurrentUid); // 나갔다는 통보 전달
                _party.SetSolo(); // 로컬에서 자신이 나가기
            }
        }


        private async void OnDm(string senderUid, DMType type, string payload)
        {
            switch (type)
            {
                case DMType.Invite:
                    if (_party.IsInParty && _party.IsLeader) return;
                    await _followerMatch.JoinInviteAsync(payload);
                    break;

                case DMType.Out:
                    if (_party.IsInParty && _party.IsLeader) return;
                    await _followerMatch.LeaveNowAsync();
                    break;

                case DMType.Recall:
                    if (_party.IsInParty && _party.IsLeader) return;
                    await _followerMatch.ReturnHomeAsync(payload);
                    break;

                case DMType.Cancel:
                    await _followerMatch.ReturnHomeAsync(GetCurrentHome());
                    break;


                case DMType.PartyInvite:
                    var pInvite = JsonUtility.FromJson<PartyInviteMsg>(payload);
                    if (_party.IsInParty) // 파티에 이미 있으면 거절
                    {
                        _chat.SendPartyReject(pInvite.leaderUid, pInvite.partyId, CurrentUid, "user_declined_already_in_party");
                        return;
                    }
                    if (senderUid == CurrentUid) return; // 자기 자신은 return
                    _partyJoinPanel.gameObject.SetActive(true);
                    _partyJoinPanel.Init(senderUid, payload);
                    break;

                case DMType.PartyAccept:
                    if (!_party.IsLeader) return; // 리더만 처리
                    //if (!_party.IsInParty) _party.SetParty(CurrentUid, _party.Members);
                    var pAccept = JsonUtility.FromJson<PartyAcceptMsg>(payload);
                    if (!_party.Members.Contains(senderUid)) _party.AddMember(senderUid); // 파티에 멤버 추가
                    BroadcastPartySync(); // 이후 파티 브로드캐스트
                    break;

                case DMType.PartyReject:
                    if (!_party.IsLeader) return; // 리더만 처리
                    // 일단 지금 당장은 할 일은 없는 듯
                    break;

                case DMType.PartySync:
                    HandlePartySync(senderUid, payload);
                    break;


                case DMType.PartyLeave:
                    if (!_party.IsLeader) break; // 리더만 멤버 제거 후 브로드캐스트
                    var pLeave = JsonUtility.FromJson<PartyLeaveMsg>(payload);
                    if (_party.IsMember(pLeave.leaverUid))
                    {
                        _party.RemoveMember(pLeave.leaverUid);
                        if (_party.HasOnlyLeader) // 나간 시점에 파티에 리더 혼자면
                        {
                            Debug.Log("파티에 리더 혼자만 남아 자동 해산");
                            _party.SetSolo();
                        }
                        else
                        {
                            // 남은 멤버들에게 파티 상태 브로드캐스트
                            BroadcastPartySync();
                        }
                    }
                    break;

                case DMType.PartyDisband:
                    var pDisband = JsonUtility.FromJson<PartyDisbandMsg>(payload);
                    _party.SetSolo(); // 해산 통보 받고 알아서 솔로 전환
                    break;
            }
        }

        private void HandlePartySync(string senderUid, string payloadJson)
        {
            Debug.Log($"{senderUid}에게서 HandlePartySync 수신 받음.");

            var msg = JsonUtility.FromJson<PartySyncMsg>(payloadJson);

            Debug.Log($"[HandlePartySync]  리더: {msg.leaderUid}, 멤버: [{string.Join(",", msg.members ?? new string[0])}]");

            _party.SetParty(msg.leaderUid, msg.members?.ToList()); // 파티 상태 동기화
        }

        private void BroadcastPartySync()
        {
            if (!_party.IsLeader) return;

            string partyId = _party.CurrentPartyId;
            string leaderUid = _party.LeaderUid;
            string[] members = _party.Members.ToArray();

            Debug.Log($"[BroadcastPartySync] leader: {leaderUid}, members: [{string.Join(",", members)}]");

            foreach (var uid in members)
            {
                _chat.SendPartySync(uid, partyId, leaderUid, members);
            }
        }
    }
}
