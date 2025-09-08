using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace MSG
{
    // TODO: 모든 방에 사람이 찼을 때 띄울 투표 로직 추가
    public class GameStarter : MonoBehaviourPunCallbacks
    {
        [SerializeField] private bool _dontTryStartWhenInHomeRoom = true;  // 홈룸에서는 시작 금지
        [SerializeField] private bool _dontTryStartWhenInPartyRoom = true; // 파티룸에서는 시작 금지

        private bool _isStarted = false; // TODO: false로 다시 바꾸는 로직도 필요
        private Coroutine _voteCO;
        private float elapsed;

        public bool IsStarted => _isStarted;
        public float Elapsed => elapsed; // 투표 UI에서 몇 초 지났는지 확인하기 위해 열어둠

        public override void OnJoinedRoom()
        {
            TryStartGame();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            TryStartGame();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            TryStopVote();
        }

        private void TryStartGame()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            if (_isStarted) return;

            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

            string roomName = PhotonNetwork.CurrentRoom.Name ?? string.Empty;

            // 홈룸에서 시작하지 않도록 방어, 파티룸에서는 인원이 다 차면 시도할 수 있음
            if (_dontTryStartWhenInHomeRoom && IsHomeRoom(roomName))
            {
                Debug.Log("홈룸인데 시작 시도되어 return");
                return;
            }

            if (_dontTryStartWhenInPartyRoom && IsPartyRoom(roomName))
            {
                Debug.Log("파티룸인데 시작 시도되어 return");
                return;
            }

            // 방이 가득 찼는지 확인
            if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                Debug.Log($"PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}, MaxPlayers: {PhotonNetwork.CurrentRoom.MaxPlayers}");
                foreach (var p in PhotonNetwork.CurrentRoom.Players)
                {
                    Debug.Log($"방에 있는 인원의 uid: {p.Value.UserId}");
                }
                StartGame();
                return;
            }
        }

        private void TryStopVote()
        {
            if (_voteCO != null)
            {
                StopCoroutine(_voteCO);
                _voteCO = null;

                _isStarted = false;
            }
        }

        private void StartGame()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
            }

            _isStarted = true;

            if (_voteCO != null)
            {
                StopCoroutine(_voteCO);
                _voteCO = null;
            }
            _voteCO = StartCoroutine(VoteRoutine());
        }

        private IEnumerator VoteRoutine() // TODO: 투표 로직 추가
        {
            // 임시 로직
            Debug.Log("투표 시작");
            yield return new WaitForSeconds(5f);
            PhotonNetwork.LoadLevel(2); // 임의로 2

            // 실제 투표 로직

            //투표UI.SetActive(true);

            //PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "Vote", null } }); // 투표 초기화

            //elapsed = 0f;
            //while (elapsed < RoomMakeHelper.VOTE_TIME)
            //{
            //    elapsed += Time.deltaTime;
            //}

            //if (PhotonNetwork.IsMasterClient)
            //{
            //    List<int> votes = new();

            //    foreach (var player in PhotonNetwork.CurrentRoom.Players)
            //    {
            //        if (player.Value.CustomProperties.TryGetValue("Vote", out object voteObj))
            //        {
            //            int voteIndex = (int)voteObj;
            //            votes.Add(voteIndex);
            //        }
            //    }

            //    // 투표가 0개면 임의 선정
            //    if (votes.Count == 0)
            //    {
            //        int randomMap = Random.Range(0, 3); // 맵 3개라 가정

            //        ExitGames.Client.Photon.Hashtable randomlyChosen = new();
            //        randomlyChosen["Vote"] = randomMap;

            //        PhotonNetwork.CurrentRoom.SetCustomProperties(randomlyChosen);

            //        yield break;
            //    }

            //    // 각 맵별 투표 수 집계
            //    Dictionary<int, int> counts = new();
            //    foreach (int v in votes)
            //    {
            //        if (!counts.ContainsKey(v)) counts[v] = 0;
            //        counts[v]++;
            //    }

            //    // 최다 득표수 계산
            //    int maxCount = counts.Values.Max();

            //    // 최다 득표 맵 후보들만 추출
            //    List<int> winners = new();
            //    foreach (var kv in counts)
            //    {
            //        if (kv.Value == maxCount)
            //            winners.Add(kv.Key);
            //    }

            //    // 동률이면 랜덤으로 하나 뽑기
            //    int chosen = winners[Random.Range(0, winners.Count)];

            //    ExitGames.Client.Photon.Hashtable chosenProp = new();
            //    chosenProp["Vote"] = chosen;
            //    PhotonNetwork.CurrentRoom.SetCustomProperties(chosenProp);

            //    PhotonNetwork.LoadLevel(2); // 임의로 2
            //}
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