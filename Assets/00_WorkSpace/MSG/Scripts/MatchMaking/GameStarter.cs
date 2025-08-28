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
        [Header("Game Start")]
        [SerializeField] private bool _startWhenRoomIsFull = true;   // 방이 가득 찼을 때만 시작
        [SerializeField] private bool _startOnlyInMatchRoom = true;  // 매치룸에서만 시작 (홈룸 말고)

        [Header("Refs")]
        [SerializeField] private PartyService _party;


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
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

            string roomName = PhotonNetwork.CurrentRoom.Name ?? string.Empty;

            // 홈룸에서 시작하지 않도록 방어
            if (_startOnlyInMatchRoom && !roomName.StartsWith("m_"))
            {
                Debug.Log("홈룸이 아닌데 시작 시도되어 return");
                return;
            }

            // 방이 가득 찼는지 확인
            if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
            {
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

            PhotonNetwork.LoadLevel(2); // 임의로 2
        }
    }
}