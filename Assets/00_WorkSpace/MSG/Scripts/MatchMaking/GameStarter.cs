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
        [SerializeField] private PartyService _party;

        [SerializeField] private bool _dontTryStartWhenInHomeRoom = true;  // 매치룸에서만 시작 (홈룸 말고)

        private bool _isStarted = false; // TODO: false로 다시 바꾸는 로직도 필요

        public bool IsStarted => _isStarted;


        public override void OnJoinedRoom()
        {
            TryStartGame();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            TryStartGame();
        }

        private void TryStartGame()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

            string roomName = PhotonNetwork.CurrentRoom.Name ?? string.Empty;

            // 홈룸에서 시작하지 않도록 방어, 파티룸에서는 인원이 다 차면 시도할 수 있음
            if (_dontTryStartWhenInHomeRoom && roomName.StartsWith("h_")) // 프로퍼티로 바꾸는 것이 좋을 듯
            {
                Debug.Log("홈룸인데 시작 시도되어 return");
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

        private void StartGame()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
            }

            _isStarted = true;

            StartCoroutine(VoteRoutine());
        }

        private IEnumerator VoteRoutine() // TODO: 투표 로직 추가
        {
            Debug.Log("투표 시작");
            yield return new WaitForSeconds(5f);
            PhotonNetwork.LoadLevel(2); // 임의로 2
        }

        // TODO: 투표 중 인원이 나갔을 때, 즉시 투표 종료하는 기능 추가
        private void StopWhileVoting()
        {

        }
    }
}