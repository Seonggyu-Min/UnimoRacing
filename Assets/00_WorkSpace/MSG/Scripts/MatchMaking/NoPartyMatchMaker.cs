using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSG
{
    public class NoPartyMatchMaker : MonoBehaviourPunCallbacks
    {
        [SerializeField] private int _maxPlayers = 2;
        [SerializeField] private int _gameLevel = 2;

        private Coroutine _voteCO;

        public Action OnActionWaitPlayer;
        public Action OnActionMatchReady;
        public Action OnActionRace;

        public void OnClickTryQuickMatch()
        {
            Debug.Log($"[NoPartyMatchMaker] 방 참가 시도");
            PhotonNetwork.JoinRandomRoom();
        }

        public void OnClickCancelMatch()
        {
            Debug.Log($"[NoPartyMatchMaker] 매칭 취소");
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[NoPartyMatchMaker] 방 참가 성공");
            DecideToShowVoteUI();

            OnActionWaitPlayer?.Invoke();
            Debug.Log($"[NoPartyMatchMaker] AutomaticallySyncScene ? {PhotonNetwork.AutomaticallySyncScene}");
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
                MaxPlayers = _maxPlayers,
                IsVisible = true,
                IsOpen = true
            };

            PhotonNetwork.CreateRoom(null, options);
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
            Debug.Log("[NoPartyMatchMaker] 투표 시작");
            // 투표 UI 활성화
            StartVoteCount();
        }

        private void DisableVoteUI()
        {
            Debug.Log("[NoPartyMatchMaker] 투표 종료");
            // 투표 UI 비활성화
            StopVoteCount();
        }

        private void StartVoteCount()
        {
            if (_voteCO != null)
            {
                StopCoroutine(_voteCO);
                _voteCO = null;
            }
            _voteCO = StartCoroutine(StartAfterVoteRoutine());
        }

        private void StopVoteCount()
        {
            if (_voteCO != null)
            {
                StopCoroutine(_voteCO);
                _voteCO = null;
            }
        }

        // 10초가 지난 후 
        private IEnumerator StartAfterVoteRoutine()
        {
            // 추후 씬 매니저로 변경
            if (!PhotonNetwork.IsMasterClient) yield break;

            float elapsed = 0f;

            Debug.Log("10초간 투표 시작");
            OnActionMatchReady?.Invoke();
            while (elapsed < 10f)
            {
                elapsed += Time.deltaTime;

                if(elapsed > 5.0f)

                yield return null;
            }
            OnActionRace?.Invoke();

            // TODO: 투표 집계 로직 추가

            // TODO: 맵 ID가 -1 아닐 때 확인
            // (RoomKey.MatchRaceMapId로 룸커스텀 프롬퍼티 확인 필요)
            
            PhotonNetwork.LoadLevel(_gameLevel);
        }

        [ContextMenu("DebugRoom")]
        public void DebugRoom()
        {
            foreach (var p in PhotonNetwork.CurrentRoom.Players)
            {
                Debug.Log($"[NoPartyMatchMaker] 현재 방 {PhotonNetwork.CurrentRoom.Name}에 {p.Value.UserId} 이 들어와있음");
            }

            Debug.Log($"[NoPartyMatchMaker] 현재 방 {PhotonNetwork.CurrentRoom.Name}에 총원 {PhotonNetwork.CurrentRoom.PlayerCount}명");
        }
    }
}
