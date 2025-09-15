using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace MSG
{
    public class LeaderMatchFinder : MonoBehaviourPunCallbacks
    {
        [SerializeField] private RoomAgent _room;
        [SerializeField] private ChatDM _chat;
        [SerializeField] private GameStarter _gameStarter;

        public async Task StartQuickMatchAsync(int partySize)
        {
            if (_gameStarter.IsStarted)
            {
                Debug.LogWarning($"이미 게임이 시작되어 return");
                return;
            }

            // 랜덤 방에서 파티 인원 만큼 자리 찾기
            bool ok = await _room.TryJoinRandomWithSlotAsync(partySize);
            if (!ok)
            {
                // 실패면 후보 방 생성
                string candidate = RoomMakeHelper.MatchCandidate();
                await _room.JoinOrCreateAsync(candidate, RoomMakeHelper.MakeMatchOptions());
            }

            // 방이 여유 있는지 다시 체크 후 초대
            int free = PhotonNetwork.CurrentRoom.MaxPlayers - PhotonNetwork.CurrentRoom.PlayerCount;
            if (free >= partySize/* - 1*/)
            {
                foreach (string uid in PartyService.Instance.Members)
                {
                    if (uid == PartyService.Instance.LeaderUid) continue;
                    _chat.SendInvite(uid, PhotonNetwork.CurrentRoom.Name);
                    Debug.Log($"CurrentRoom.Name = {PhotonNetwork.CurrentRoom.Name}");
                }
            }
        }

        public async void CancelMatch()
        {
            // 파티원들 복귀 명령
            foreach (string uid in PartyService.Instance.Members)
            {
                if (uid == PartyService.Instance.LeaderUid) continue;
                _chat.SendCancel(uid);
            }

            await _room.EnsureHomeRoomAsync(RoomMakeHelper.PartyHome(PartyService.Instance.LeaderUid)); // 자신도 복귀
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            CheckAllMembersIncluded();
        }

        private async void CheckAllMembersIncluded()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            var room = PhotonNetwork.CurrentRoom.CustomProperties;
            bool isMatchRoom = room.TryGetValue(RoomMakeHelper.ROOM_TYPE, out object roomType) && (RoomType)roomType == RoomType.Match;
            if (!isMatchRoom) return; // 매치용 룸이 아니면 return

            if (PhotonNetwork.CurrentRoom.MaxPlayers - PhotonNetwork.CurrentRoom.PlayerCount != 0) return; // 방 인원이 다 차지 않았으면 return

            List<string> memberUids = new();
            foreach (var player in PhotonNetwork.CurrentRoom.Players)
            {
                if (PartyService.Instance.Members.Contains(player.Value.UserId))
                {
                    memberUids.Add(player.Value.UserId);
                }
            }

            bool same = new HashSet<string>(PartyService.Instance.Members).SetEquals(memberUids);

            // 모든 멤버가 방에 있지 않으면
            if (!same)
            {
                // 즉시 OUT하여 파티원이 다른 방에 있게하지 말기
                foreach (string uid in PartyService.Instance.Members)
                {
                    if (uid == PartyService.Instance.LeaderUid) continue;
                    _chat.SendOut(uid);
                }

                string newMatchRoom = RoomMakeHelper.MatchCandidate();
                await _room.JoinOrCreateAsync(newMatchRoom, RoomMakeHelper.MakeMatchOptions());

                foreach (string uid in PartyService.Instance.Members)
                {
                    if (uid == PartyService.Instance.LeaderUid) continue;
                    _chat.SendInvite(uid, newMatchRoom);
                }
            }
        }
    }
}
