using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace MSG
{
    public class LeaderMatchFinder : MonoBehaviour
    {
        [SerializeField] private PartyService _party;
        [SerializeField] private RoomAgent _room;
        [SerializeField] private ChatDM _chat;

        public async Task StartQuickMatchAsync(int partySize)
        {
            // 랜덤 방에서 파티 인원 만큼 자리 찾기
            bool ok = await _room.TryJoinRandomWithSlotAsync(partySize);
            if (!ok)
            {
                // 실패면 후보 방 생성
                string candidate = RoomMakeHelper.MatchCandidate();
                await _room.JoinOrCreateAsync(candidate, MakeCandidateOptions());
            }

            // 방이 여유 있는지 다시 체크 후 초대
            int free = Photon.Pun.PhotonNetwork.CurrentRoom.MaxPlayers - Photon.Pun.PhotonNetwork.CurrentRoom.PlayerCount;
            if (free >= partySize)
            {
                foreach (string uid in _party.Members)
                {
                    if (uid == _party.LeaderUid) continue;
                    _chat.SendInvite(uid, Photon.Pun.PhotonNetwork.CurrentRoom.Name);
                }
            }
            else
            {
                // 가득 차면 즉시 OUT 후 RECALL
                foreach (string uid in _party.Members)
                {
                    if (uid == _party.LeaderUid) continue;
                    _chat.SendOut(uid);
                }
                string home = RoomMakeHelper.PartyHome(_party.LeaderUid);
                foreach (string uid in _party.Members)
                {
                    if (uid == _party.LeaderUid) continue;
                    _chat.SendRecall(uid, home);
                }
                // 리더도 홈 복귀
                await _room.EnsureHomeRoomAsync(home);
            }
        }

        public void CancelMatch()
        {
            foreach (string uid in _party.Members)
            {
                if (uid == _party.LeaderUid) continue;
                _chat.SendCancel(uid);
            }
        }

        private static RoomOptions MakeCandidateOptions()
        {
            RoomOptions opts = new RoomOptions();
            opts.PublishUserId = true;
            opts.IsOpen = true;
            opts.IsVisible = true;
            opts.MaxPlayers = RoomMakeHelper.MAX_PLAYERS;
            return opts;
        }
    }
}
