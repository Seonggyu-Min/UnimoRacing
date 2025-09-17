using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace MSG.Deprecated
{
    public class PhotonCallbackHandler : MonoBehaviourPunCallbacks
    {
        [SerializeField] private MatchClient matchClient;
        [SerializeField] private int maxPlayers = 4;

        private string _roomName;
        private long _roomCreatedAt;
        private (string partyId, string leaderUid, List<string> uids) _cachedPartyInfo;
        private bool _isInitiated = false;
        private int _createAttempts = 0;
        private Coroutine _recreateRoomCO;


        private void Start()
        {
            if (PhotonNetwork.IsConnected || !_isInitiated)
            {
                StartCoroutine(CreateRoomWithWhitelistCoroutine());
            }
        }

        public override void OnConnectedToMaster()
        {
            // 코루틴으로 파티 정보 fetch -> CreateRoom 진행
            Debug.Log("[PUN] 마스터 서버 연결 성공");
            StartCoroutine(CreateRoomWithWhitelistCoroutine());
        }

        private IEnumerator CreateRoomWithWhitelistCoroutine()
        {
            if (_isInitiated) yield break;
            _isInitiated = true;

            var uid = FirebaseManager.Instance.Auth.CurrentUser.UserId;

            // 비동기 파티 정보 로드
            var infoTask = FetchPartyInfo(uid);
            while (!infoTask.IsCompleted) yield return null;

            if (infoTask.Exception != null)
            {
                Debug.LogWarning($"[PUN] 파티를 읽을 수 없어 솔로로 전환합니다 {infoTask.Exception}");
                _cachedPartyInfo = (null, uid, new List<string> { uid });
            }
            else
            {
                var info = infoTask.Result;
                _cachedPartyInfo = (
                    partyId: info.partyId ?? $"solo-{uid}",
                    leaderUid: info.leaderUid ?? uid,
                    uids: info.uids ?? new List<string> { uid }
                );
            }

            // 매치용 비공개 방 생성
            _roomName = $"rm-{Guid.NewGuid():N}";
            _roomCreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var opts = new RoomOptions
            {
                MaxPlayers = maxPlayers,
                IsVisible = false,
                PublishUserId = true
            };

            // 초기 ExpectedUsers = 파티 멤버, 없으면 솔로
            var initialWhitelist = _cachedPartyInfo.uids ?? new List<string> { uid };

            PhotonNetwork.CreateRoom(_roomName, opts, TypedLobby.Default, initialWhitelist.ToArray());
            Debug.Log($"[PUN] 방 생성: '{_roomName}', 화이트리스트: {string.Join(",", initialWhitelist)}");
        }

        public override async void OnCreatedRoom()
        {
            Debug.Log($"[PUN] OnCreatedRoom: {_roomName}");

            _createAttempts = 0;
            StopRecreateRoom();

            // 캐시된 파티정보가 있으면 재사용, 없으면 한번 더 조회
            var uid = FirebaseManager.Instance.Auth.CurrentUser.UserId;
            var info = _cachedPartyInfo.uids != null ? _cachedPartyInfo : await FetchPartyInfo(uid);

            var partyId = info.partyId ?? $"solo-{uid}";
            var contactUid = info.leaderUid ?? uid;
            var anchorUid = uid;
            var uids = info.uids ?? new List<string> { uid };
            var size = uids.Count;
            var max = maxPlayers;

            // MatchClient에 초기 LFG 정보 주입
            matchClient.SetLocalPartyRoom(
                partyId: partyId,
                room: _roomName,
                contactUid: contactUid,
                anchorUid: anchorUid,
                uids: uids,
                size: size,
                max: max,
                roomCreatedAt: _roomCreatedAt
            );
            // MatchClient는 Chat 연결 후 _me 세팅 기다렸다가 LFG 발행
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[PUN] CreateRoom failed {returnCode} {message}");
            StartRecreateRoom();
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("[PUN] OnJoinedRoom");
            // 게스트 티켓 조인 성공 시 코루틴 중단 등 처리
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[PUN] JoinRoom failed {returnCode} {message}");
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"[PUN] 게스트 입장: {newPlayer.UserId}. 현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

            if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                // TODO: 투표 시작, 타이머 시작
                Debug.Log("[게임] 정원 달성, 투표 시작");
            }
        }

        // Firebase 관련
        private async Task<(string partyId, string leaderUid, List<string> uids)> FetchPartyInfo(string uid)
        {
            // 소속 파티 ID
            var partyIdSnap = await FirebaseManager.Instance.Database
                .GetReference(DBRoutes.PartyMembership(uid))
                .GetValueAsync();

            string partyId = null;
            if (partyIdSnap.Exists && partyIdSnap.Value != null)
                partyId = partyIdSnap.Value.ToString();

            if (string.IsNullOrEmpty(partyId))
            {
                // 솔로
                return (null, null, null);
            }

            var membersSnap = await FirebaseManager.Instance.Database
                .GetReference(DBRoutes.PartyMembers(partyId))
                .GetValueAsync();

            List<string> members = new List<string>();
            foreach (var child in membersSnap.Children)
            {
                if (!string.IsNullOrEmpty(child.Key))
                    members.Add(child.Key);
            }

            // 리더
            var leaderSnap = await FirebaseManager.Instance.Database
                .GetReference(DBRoutes.PartyLeader(partyId))
                .GetValueAsync();

            string leaderUid = leaderSnap.Exists && leaderSnap.Value != null
                ? leaderSnap.Value.ToString()
                : (members.Count > 0 ? members[0] : uid);

            return (partyId, leaderUid, members);
        }

        private IEnumerator RecreateRoomRoutine()
        {
            _createAttempts++;

            while (_createAttempts < 10)
            {
                CreateRoom();
                yield return new WaitForSeconds(Mathf.Min(0.3f * _createAttempts, 1.5f));
            }
        }

        private void StartRecreateRoom()
        {
            if (_recreateRoomCO != null)
            {
                StopCoroutine(_recreateRoomCO);
                _recreateRoomCO = null;
            }
            _recreateRoomCO = StartCoroutine(RecreateRoomRoutine());
        }

        private void StopRecreateRoom()
        {
            if (_recreateRoomCO != null)
            {
                StopCoroutine(_recreateRoomCO);
                _recreateRoomCO = null;
            }
        }

        private void CreateRoom()
        {
            var uid = FirebaseManager.Instance.Auth.CurrentUser.UserId;

            _roomName = $"rm-{Guid.NewGuid():N}";
            _roomCreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var opts = new RoomOptions
            {
                MaxPlayers = maxPlayers,
                IsVisible = false,
                PublishUserId = true,
            };

            var initialWhitelist = _cachedPartyInfo.uids ?? new List<string> { uid };

            PhotonNetwork.CreateRoom(_roomName, opts, TypedLobby.Default, initialWhitelist.ToArray());
            Debug.Log($"[PUN] Creating room: '{_roomName}', whitelist: {string.Join(",", initialWhitelist)}");
        }
    }
}
