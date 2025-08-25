using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSG
{
    public class NoPartyMatchMaker : MonoBehaviourPunCallbacks
    {
        public void OnClickTryQuickMatch()
        {
            Debug.Log($"[NoPartyMatchMaker] 방 참가 시도");
            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[NoPartyMatchMaker] 방 참가 성공");
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log($"[NoPartyMatchMaker] 방 참가 실패");
            CreateRoom();
        }

        public void CreateRoom()
        {
            Debug.Log($"[NoPartyMatchMaker] 방 생성 시도");

            RoomOptions options = new RoomOptions
            {
                MaxPlayers = 4,
                IsVisible = true,
                IsOpen = true
            };

            PhotonNetwork.CreateRoom(null, options, TypedLobby.Default);
        }

        public override void OnCreatedRoom()
        {
            Debug.Log($"[NoPartyMatchMaker] 방 생성 성공");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.Log($"[NoPartyMatchMaker] 방 생성 실패, message: {message}");
            // 예외 처리
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            DecideToShowVoteUI();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            DecideToShowVoteUI();
        }

        private void DecideToShowVoteUI()
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                ShowVoteUI();
            }
            else
            {
                DisableVoteUI();
            }
        }

        private void ShowVoteUI()
        {
            // 투표 UI 활성화
        }

        private void DisableVoteUI()
        {
            // 투표 UI 비활성화
        }
    }
}
