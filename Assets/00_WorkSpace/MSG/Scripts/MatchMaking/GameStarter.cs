using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace MSG
{
    public static class RoomStateKeys
    {
        public const string active = "active";
        public const string inactive = "inactive";
        public const string ended = "ended";
    }

    public class GameStarter : MonoBehaviourPunCallbacks
    {
        [SerializeField] private bool _dontTryStartWhenInHomeRoom = true;  // 홈룸에서는 시작 금지
        [SerializeField] private bool _dontTryStartWhenInPartyRoom = true; // 파티룸에서는 시작 금지

        private bool _isStarted = false;
        private Coroutine _voteCO;

        public bool IsStarted => _isStarted;


        public override void OnJoinedRoom()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                TryStartVoteAsMaster();
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                TryStartVoteAsMaster();
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (!_isStarted) return;

            AbortVoteAndReopenRoom();
        }

        public override void OnLeftRoom()
        {
            StopVoteRoutine();
            _isStarted = false;
        }


        private void TryStartVoteAsMaster()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (_isStarted) return;
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

            string roomName = PhotonNetwork.CurrentRoom.Name ?? string.Empty;

            if (_dontTryStartWhenInHomeRoom && IsHomeRoom(roomName))
                return;

            if (_dontTryStartWhenInPartyRoom && IsPartyRoom(roomName))
                return;

            // 방이 가득 찼을 때만 투표 시작
            if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                // 방 닫기
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;

                _isStarted = true;

                Debug.Log($"공지 이전 룸 State: {PhotonNetworkCustomProperties.GetRoomProp<string>(RoomKey.VoteState)}");

                // 룸 공지
                double endAt = PhotonNetwork.Time + RoomMakeHelper.VOTE_TIME;
                var roomProps = new Hashtable
                {
                    { PhotonNetworkCustomProperties.KEY_VOTE_WINNER_INDEX, -1 },
                    { PhotonNetworkCustomProperties.KEY_ROOM_VOTE_STATE, RoomStateKeys.active },
                    { PhotonNetworkCustomProperties.KEY_ROOM_VOTE_END_AT, endAt }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

                Debug.Log($"룸 공지 됨 {RoomStateKeys.active}, endsAt: {endAt}, PhotonNetwork.Time: {PhotonNetwork.Time}");

                StopVoteRoutine();
                _voteCO = StartCoroutine(VoteRoutineMaster(endAt));
            }
        }

        private IEnumerator VoteRoutineMaster(double endAt)
        {
            Debug.Log("투표 대기 중");
            // 타임아웃 대기
            while (PhotonNetwork.Time < endAt)
            {
                yield return null;
            }

            // 집계
            Dictionary<int, int> counts = new Dictionary<int, int>();
            foreach (var p in PhotonNetwork.CurrentRoom.Players.Values)
            {
                if (p.CustomProperties.TryGetValue(PhotonNetworkCustomProperties.KEY_VOTE_MAP, out var pv) && pv is int idx && idx >= 1)
                {
                    if (!counts.ContainsKey(idx)) counts[idx] = 0;
                    counts[idx]++;
                }
            }

            // 승자 결정
            int winner;
            if (counts.Count == 0)
            {
                // 아무도 안 찍었으면 랜덤
                winner = Random.Range(1, 4);
            }
            else
            {
                int max = counts.Values.Max();
                List<int> winners = new();
                foreach (var kv in counts)
                {
                    if (kv.Value == max)
                    {
                        winners.Add(kv.Key);
                    }
                }

                winner = winners[Random.Range(0, winners.Count)];
            }

            // 룸 상태 종료 설정 및 승자 설정
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { PhotonNetworkCustomProperties.KEY_VOTE_WINNER_INDEX, winner },
                { PhotonNetworkCustomProperties.KEY_ROOM_VOTE_STATE, RoomStateKeys.ended }
            });

            Debug.Log($"투표 집계 완료: {winner}");

            // 씬 로드
            yield return new WaitForSeconds(1f); // UI가 어색해서 일단 1초 대기
            Debug.Log("로드 레벨 호출됨");
            PhotonNetwork.LoadLevel(2);
        }

        // 투표 중단 및 방 오픈
        private void AbortVoteAndReopenRoom()
        {
            // 초기화
            var roomProps = new Hashtable
            {
                { PhotonNetworkCustomProperties.KEY_ROOM_VOTE_STATE, RoomStateKeys.inactive },
                { PhotonNetworkCustomProperties.KEY_VOTE_WINNER_INDEX, -1 },
                { PhotonNetworkCustomProperties.KEY_ROOM_VOTE_END_AT, 0 }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;

            StopVoteRoutine();
            _isStarted = false;
        }

        private void StopVoteRoutine()
        {
            if (_voteCO != null)
            {
                StopCoroutine(_voteCO);
                _voteCO = null;
            }
        }

        private bool IsHomeRoom(string roomName)
        {
            if (roomName.StartsWith("h_")) return true; // 혹시 몰라서 방 이름으로도 검사 넣음

            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomMakeHelper.ROOM_TYPE, out object roomType)
                && roomType is string roomTypeString)
            {
                return roomTypeString == "Home";
            }

            return false;
        }

        private bool IsPartyRoom(string roomName)
        {
            if (roomName.StartsWith("p_")) return true; // 혹시 몰라서 방 이름으로도 검사 넣음

            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomMakeHelper.ROOM_TYPE, out object roomType)
                && roomType is string roomTypeString)
            {
                return roomTypeString == "Party";
            }

            return false;
        }
    }
}