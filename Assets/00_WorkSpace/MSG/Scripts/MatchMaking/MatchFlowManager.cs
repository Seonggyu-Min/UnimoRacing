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
        [SerializeField] private RoomAgent _room;
        [SerializeField] private ChatDM _chat;
        [SerializeField] private LeaderMatchFinder _leaderMatch;
        [SerializeField] private FollowerMatchHandler _followerMatch;
        [SerializeField] private GameStarter _gameStarter;

        [SerializeField] private PartyJoinPanel _partyJoinPanel;

        private bool _initialized = false;
        private bool _isJoiningInvite = false;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;


        private async void Start()
        {
            _chat.Initialize(CurrentUid);

            _chat.OnDirectMessageReceived += OnDm;
            PartyService.Instance.OnPartyChanged += OnPartyChanged;

            //if (!_initialized)
            //{
            //    PartyService.Instance.SetSolo();
            //    _initialized = true;
            //}

            await _room.EnsureHomeRoomAsync(GetCurrentHome());
        }

        private async void OnPartyChanged()
        {
            if (_gameStarter.IsStarted)
            {
                Debug.LogWarning("[OnPartyChanged] 게임 중에 파티가 변경되어 return");
                return;
            }
            if (_isJoiningInvite && PartyService.Instance.IsInParty)
            {
                Debug.LogWarning("[OnPartyChanged] 초대에 의한 참여 중에 파티가 변경되어 return");
                return;
            }

            string home = GetCurrentHome();

            Debug.Log($"[OnPartyChanged] IsInParty: {PartyService.Instance.IsInParty}, Leader: {PartyService.Instance.LeaderUid}, home: {home}");

            await _room.EnsureHomeRoomAsync(home);
        }

        private void OnDestroy()
        {
            _chat.OnDirectMessageReceived -= OnDm;
            PartyService.Instance.OnPartyChanged -= OnPartyChanged;
        }

        private string GetCurrentHome()
        {
            if (PartyService.Instance.IsInParty)
            {
                return RoomMakeHelper.PartyHome(PartyService.Instance.LeaderUid);
            }
            return RoomMakeHelper.Personal(CurrentUid);
        }


        public async void OnClickQuickMatch()
        {
            // 파티가 아닌 경우에는 바로 퀵매치
            if (!PartyService.Instance.IsInParty)
            {
                Debug.Log("솔로로 매치 시작");
                await _leaderMatch.StartQuickMatchAsync(0); // 랜덤방 참여 시도 후 실패 시 후보방 생성
                return;
            }

            // 파티인 경우는 리더만 시작
            if (!PartyService.Instance.IsLeader)
            {
                Debug.Log("리더만 시작 가능");
                return;
            }

            int partySize = PartyService.Instance.Members.Count;
            await _leaderMatch.StartQuickMatchAsync(partySize - 1);
        }

        public async void OnClickCancelMatch()
        {
            if (PartyService.Instance.IsInParty && PartyService.Instance.IsLeader)
            {
                _leaderMatch.CancelMatch();
            }
            else if (!PartyService.Instance.IsInParty)
            {
                await _followerMatch.ReturnHomeAsync(RoomMakeHelper.Personal(CurrentUid));
            }
        }

        public void OnClickLeaveParty()
        {
            if (!PartyService.Instance.IsInParty)
            {
                Debug.Log("파티에 있지 않습니다");
                return;
            }

            // 리더는 파티 탈퇴 시 해산
            if (PartyService.Instance.IsLeader)
            {
                // 모든 멤버에게 해산 통지
                foreach (var uid in PartyService.Instance.Members)
                {
                    if (uid == PartyService.Instance.LeaderUid) continue;
                    _chat.SendPartyDisband(uid, PartyService.Instance.LeaderUid);
                }

                PartyService.Instance.SetSolo(); // 리더 자신도 나가기
            }
            // 팔로워는 파티 탈퇴 시 혼자만 나감
            else
            {
                _chat.SendPartyLeave(PartyService.Instance.LeaderUid, CurrentUid); // 나갔다는 통보 전달
                PartyService.Instance.SetSolo(); // 로컬에서 자신이 나가기
            }
        }


        private async void OnDm(string senderUid, DMType type, string payload)
        {
            switch (type)
            {
                case DMType.Invite:
                    if (PartyService.Instance.IsInParty && PartyService.Instance.IsLeader) return;
                    _isJoiningInvite = true;
                    try { await _followerMatch.JoinInviteAsync(payload); }
                    finally { _isJoiningInvite = false; }
                    break;

                case DMType.Out:
                    if (PartyService.Instance.IsInParty && PartyService.Instance.IsLeader) return;
                    await _followerMatch.LeaveNowAsync();
                    break;

                case DMType.Recall:
                    if (PartyService.Instance.IsInParty && PartyService.Instance.IsLeader) return;
                    await _followerMatch.ReturnHomeAsync(payload);
                    break;

                case DMType.Cancel:
                    await _followerMatch.ReturnHomeAsync(GetCurrentHome());
                    break;


                case DMType.PartyInvite:
                    var pInvite = JsonUtility.FromJson<PartyInviteMsg>(payload);
                    if (PartyService.Instance.IsInParty) // 파티에 이미 있으면 거절
                    {
                        _chat.SendPartyReject(pInvite.leaderUid, pInvite.partyId, CurrentUid, "user_declined_already_in_party");
                        return;
                    }
                    if (senderUid == CurrentUid) return; // 자기 자신은 return
                    _partyJoinPanel.gameObject.SetActive(true);
                    _partyJoinPanel.Init(senderUid, payload);
                    break;

                case DMType.PartyAccept:
                    if (!PartyService.Instance.IsLeader) return; // 리더만 처리
                    //if (!_party.IsInParty) _party.SetParty(CurrentUid, _party.Members);
                    var pAccept = JsonUtility.FromJson<PartyAcceptMsg>(payload);
                    if (!PartyService.Instance.Members.Contains(senderUid)) PartyService.Instance.AddMember(senderUid); // 파티에 멤버 추가
                    BroadcastPartySync(); // 이후 파티 브로드캐스트
                    break;

                case DMType.PartyReject:
                    if (!PartyService.Instance.IsLeader) return; // 리더만 처리
                    // 일단 지금 당장은 할 일은 없는 듯
                    break;

                case DMType.PartySync:
                    HandlePartySync(senderUid, payload);
                    break;


                case DMType.PartyLeave:
                    if (!PartyService.Instance.IsLeader) break; // 리더만 멤버 제거 후 브로드캐스트
                    var pLeave = JsonUtility.FromJson<PartyLeaveMsg>(payload);
                    if (PartyService.Instance.IsMember(pLeave.leaverUid))
                    {
                        PartyService.Instance.RemoveMember(pLeave.leaverUid);
                        if (PartyService.Instance.HasOnlyLeader) // 나간 시점에 파티에 리더 혼자면
                        {
                            Debug.Log("파티에 리더 혼자만 남아 자동 해산");
                            PartyService.Instance.SetSolo();
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
                    PartyService.Instance.SetSolo(); // 해산 통보 받고 알아서 솔로 전환
                    break;
            }
        }

        private void HandlePartySync(string senderUid, string payloadJson)
        {
            Debug.Log($"{senderUid}에게서 HandlePartySync 수신 받음.");

            var msg = JsonUtility.FromJson<PartySyncMsg>(payloadJson);

            Debug.Log($"[HandlePartySync]  리더: {msg.leaderUid}, 멤버: [{string.Join(",", msg.members ?? new string[0])}]");

            PartyService.Instance.SetParty(msg.leaderUid, msg.members?.ToList()); // 파티 상태 동기화
        }

        private void BroadcastPartySync()
        {
            if (!PartyService.Instance.IsLeader) return;

            string partyId = PartyService.Instance.CurrentPartyId;
            string leaderUid = PartyService.Instance.LeaderUid;
            string[] members = PartyService.Instance.Members.ToArray();

            Debug.Log($"[BroadcastPartySync] leader: {leaderUid}, members: [{string.Join(",", members)}]");

            foreach (var uid in members)
            {
                _chat.SendPartySync(uid, partyId, leaderUid, members);
            }
        }
    }
}
