using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace MSG
{
    public class MatchClient : MonoBehaviourPunCallbacks
    {
        [Header("Chat")]
        [SerializeField] private ServerSettings serverSettings;
        [SerializeField] private string _region = "asia";
        [SerializeField] private string _channel = "mm-standard";
        [SerializeField] private int _history = 10;

        [Header("LFG")]
        [SerializeField] private int _lfgTtlMs = 15000;
        [SerializeField] private float _lfgIntervalSec = 3f;

        private Coroutine _joinRetryCO;
        private bool _leftRoom;
        private bool _connectedToMaster;
        private bool _joinedLobby;
        private bool _joinedTargetRoom;
        private bool _joinFailed;

        public ChatService Chat { get; private set; }
        private LfgCache _cache;
        private MatchFSM _fsm;
        private MatchMessages.LfgMsg _me;
        private float _sweepTimer;
        private const float SWEEP_INTERVAL = 10f; // 캐시 청소 주기

        // 초기 설정
        public void SetLocalPartyRoom(string partyId, string room, string leaderUid, List<string> uids, int size, int max, long roomCreatedAt)
        {
            var now = NowMs();
            _me = new MatchMessages.LfgMsg
            {
                id = Guid.NewGuid().ToString("N"),
                partyId = partyId,
                room = room,
                leaderUid = leaderUid,
                uids = uids,
                size = size,
                max = max,
                ts = now,
                expiresAt = now + _lfgTtlMs,
                roomCreatedAt = roomCreatedAt
            };
        }

        private void Start()
        {
            _cache = new LfgCache(_lfgTtlMs);

            _fsm = new MatchFSM
            {
                Publish = (ch, msg) => Chat.Publish(ch, msg),
                SendDM = (to, msg) => Chat.SendDM(to, msg),
                NowMs = NowMs,
                ChannelName = _channel,
                MyUid = FirebaseManager.Instance.Auth.CurrentUser.UserId,
                GetMyLfg = () => _me,
                FindLfgByRoom = FindLfgByRoom,

                // PUN 연동 콜백 주입
                SetExpectedUsers = SetExpectedUsersViaPun,
                TryJoinRoomUntil = TryJoinRoomUntilViaPun,
                CloseMyRoomForMerge = CloseMyRoomForMergeViaPun,
                UpdateLocalPartyAdvertised = OnUpdateLocalPartyAdvertised
            };

            Chat = new ChatService();
            Chat.Connected += OnChatConnected;
            Chat.OnChannelMessage(_channel, OnMatchChannel);
            Chat.OnDM(OnDM);
            Chat.Connect(serverSettings.AppSettings.AppIdChat, _fsm.MyUid, _region);
        }

        private void Update()
        {
            Chat?.Service();
            _fsm?.Tick(); // WAITING 타임아웃 체크

            _sweepTimer += Time.deltaTime;
            if (_sweepTimer >= SWEEP_INTERVAL)
            {
                _sweepTimer = 0f;
                _cache?.Sweep(NowMs());
            }
        }

        public override void OnDisable()
        {
            Chat?.Unsubscribe(_channel);
        }

        private void OnChatConnected()
        {
            // 채널 구독
            Chat.Subscribe(_channel, _history);

            Debug.Log($"[매치] 채팅 서버 연결 성공, 채널 구독: {_channel}");

            Invoke(nameof(StartLfgBroadcastSafely), 0.1f);
        }

        private void StartLfgBroadcastSafely()
        {
            if (_me == null)
            {
                Debug.LogWarning("[매치] 아직 파티 정보(_me)가 없어서 LFG 발행 대기 중...");
                Invoke(nameof(StartLfgBroadcastSafely), 0.2f);
                return;
            }

            Debug.Log($"[매치] LFG 발행 시작 (주기: {_lfgIntervalSec}초)");
            // 첫 LFG 즉시 발행
            BroadcastLfg();
            // 주기 브로드캐스트 시작
            InvokeRepeating(nameof(BroadcastLfg), _lfgIntervalSec, _lfgIntervalSec);
        }

        private void BroadcastLfg()
        {
            if (_me == null)
            {
                Debug.LogError("[매치] _me가 null");
                return;
            }
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning($"[매치] 방에 있지 않음, {PhotonNetwork.NetworkClientState.ToString()}");
                return;
            }

            var room = PhotonNetwork.CurrentRoom;
            if (room == null)
            {
                Debug.Log("[매치] CurrentRoom이 null");
                return;
            }

            // 마스터가 아닐 땐 광고 금지
            if (!PhotonNetwork.IsMasterClient) return;

            // merge 중이면 광고 금지
            //if (room.CustomProperties != null && room.CustomProperties.ContainsKey("merging"))
            //{
            //    var m = room.CustomProperties["merging"];
            //    if (m is bool merging && merging) return;
            //}

            // 실제 현재 방 이름으로 동기화
            if (_me.room != room.Name)
            {
                _me.room = room.Name;
            }

            // 현재 인원은 Photon에서 다시 계산
            _me.uids.Clear();
            if (room.Players != null)
            {
                foreach (var p in room.Players)
                {
                    var uid = p.Value?.UserId;
                    if (!string.IsNullOrEmpty(uid))
                    {
                        _me.uids.Add(uid);
                    }
                }
            }
            _me.size = _me.uids.Count;
            _me.max = room.MaxPlayers;

            var msg = new MatchMessages.LfgMsg
            {
                partyId = _me.partyId,
                room = room.Name,
                leaderUid = _me.leaderUid,
                uids = _me.uids.ToList(),
                size = _me.size,
                max = _me.max,
                roomCreatedAt = _me.roomCreatedAt
            };

            Debug.Log($"[LFG] 내가 방({_me.room}) 정보를 발행함. 파티ID:{_me.partyId}, 인원:{_me.size}/{_me.max}");

            foreach (var p in PhotonNetwork.CurrentRoom.Players)
            {
                Debug.Log($"[LFG] 현재 방 인원의 uid: {p.Value.UserId}");
            }
            Debug.Log($"[LFG] 현재 방 총원: {PhotonNetwork.CurrentRoom.PlayerCount}");

            Chat.Publish(_channel, msg);
        }

        private void OnMatchChannel(string sender, object raw)
        {
            var json = raw as string;
            if (string.IsNullOrEmpty(json)) return;

            if (json.Contains("\"t\":\"LFG\""))
            {
                var msg = JsonUtility.FromJson<MatchMessages.LfgMsg>(json);
                _cache.Upsert(msg, NowMs());
                Debug.Log($"[LFG] 다른 파티 LFG 수신: 파티ID:{msg.partyId}, 방:{msg.room}, 인원:{msg.size}/{msg.max}");
                _fsm.TryMatchWith(_cache);
            }
            else if (json.Contains("\"t\":\"MATCH_LOCK\""))
            {
                var m = JsonUtility.FromJson<MatchMessages.MatchLockMsg>(json);
                Debug.Log($"[LOCK] LOCK 메시지 수신 -> 호스트방:{m.hostRoom}, 게스트방:{m.guestRoom}, 매치ID:{m.matchId}");
                _fsm.OnLock(m);
            }
            else if (json.Contains("\"t\":\"MATCH_ACK\""))
            {
                var a = JsonUtility.FromJson<MatchMessages.MatchAckMsg>(json);
                Debug.Log($"[ACK] ACK 메시지 수신 -> 매치ID:{a.matchId}, 결과:{a.ok}");
                _fsm.OnAck(a);
            }
        }

        private void OnDM(string sender, object raw)
        {
            var json = raw as string;
            if (string.IsNullOrEmpty(json)) return;

            if (json.Contains("\"t\":\"TICKET\""))
            {
                var t = JsonUtility.FromJson<MatchMessages.TicketMsg>(json);
                Debug.Log($"[TICKET] 티켓 수신 -> 방:{t.room}, 만료:{t.exp}, 매치ID:{t.matchId}");
                _fsm.OnTicket(t);
            }
            else if (json.Contains("\"t\":\"MATCH_ACK\""))
            {
                var a = JsonUtility.FromJson<MatchMessages.MatchAckMsg>(json);
                _fsm.OnAck(a);
            }
        }

        // PUN 연동
        private void SetExpectedUsersViaPun(List<string> mergedUids)
        {
            Debug.Log($"[FSM] SetExpectedUsers: {string.Join(",", mergedUids)}");
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.CurrentRoom.SetExpectedUsers(mergedUids.ToArray());
            }
            else
            {
                Debug.LogWarning($"[FSM] 방에 있지 않습니다");
            }
        }

        private void TryJoinRoomUntilViaPun(string room, long expMs)
        {
            if (_joinRetryCO != null)
            {
                StopCoroutine(_joinRetryCO);
                _joinRetryCO = null;
            }
            _joinRetryCO = StartCoroutine(JoinRetry(room, expMs));
        }

        private IEnumerator JoinRetry(string room, long expMs)
        {
            int attempt = 0;

            Debug.Log("[PUN] 자신 Room에서 나올 때 까지 대기");

            if (PhotonNetwork.InRoom)
            {
                Debug.Log("[PUN] 현재 방에서 나가는 중...");
                PhotonNetwork.LeaveRoom(false);
            }
            yield return new WaitUntil(() => !PhotonNetwork.InRoom);
            Debug.Log("[PUN] 현재 방에서 나옴");

            // 로비 진입까지 대기
            _joinedLobby = PhotonNetwork.NetworkClientState == ClientState.JoinedLobby;

            Debug.Log("[PUN] 로비에 진입할 때 까지 대기");
            if (!_joinedLobby)
            {
                Debug.Log("[PUN] 로비 진입 대기...");
                PhotonNetwork.JoinLobby();
                yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.JoinedLobby);
            }

            Debug.Log($"[PUN] 현재 클라이언트 상태: {PhotonNetwork.NetworkClientState.ToString()}");

            // 유효시간 동안 Join 재시도
            while (NowMs() < expMs)
            {
                // 이미 목표 방에 있으면 종료
                if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.Name == room)
                {
                    Debug.Log("[PUN] 이미 목표 방에 입장되어 있음. 재시도 종료.");
                    yield break;
                }

                _joinedTargetRoom = false;
                _joinFailed = false;

                // 로비에 없다면 먼저 로비 진입
                if (!(PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer ||
                      PhotonNetwork.NetworkClientState == ClientState.JoinedLobby))
                {
                    Debug.Log("[PUN] 매치메이킹 가능한 상태가 아님. 로비 진입 대기");
                    yield return new WaitUntil(() =>
                        PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer ||
                        PhotonNetwork.NetworkClientState == ClientState.JoinedLobby
                    );
                }

                attempt++;
                Debug.Log($"[PUN] 방 합류 시도 {attempt}회 -> {room} (state:{PhotonNetwork.NetworkClientState})");
                PhotonNetwork.JoinRoom(room);

                // 성공/실패/타임아웃까지 대기
                float timeout = Mathf.Min(2f + 0.3f * attempt, 5f); // 최대 5초 대기 백오프
                float t = 0f;
                while (t < timeout && !_joinedTargetRoom && !_joinFailed)
                {
                    // 중간에 다른 이유로 이미 들어간 경우
                    if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.Name == room)
                    {
                        _joinedTargetRoom = true;
                        break;
                    }
                    t += Time.deltaTime;
                    yield return null;
                }

                if (_joinedTargetRoom)
                {
                    Debug.Log("[PUN] 방 합류 성공, 코루틴 종료");
                    yield break;
                }

                // 실패했으면 백오프 후 다음 시도
                Debug.Log("[PUN] 방 합류 실패 또는 타임아웃. 재시도 대기...");
                yield return new WaitForSeconds(Mathf.Min(0.3f * attempt, 1.5f));
            }

            Debug.LogWarning("[PUN] 티켓 만료: 더 이상 방 합류를 시도하지 않음");
        }

        private void CloseMyRoomForMergeViaPun()
        {
            Debug.Log("[FSM] CloseMyRoomForMerge: prevent new entries");

            // 방 안이 아니면 무시
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            {
                Debug.Log("[PUN] 내 방 닫기 스킵: 현재 방에 있지 않음");
                return;
            }

            // 새로운 입장 금지, 로비에서 숨김
            if (PhotonNetwork.CurrentRoom != null)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;

                // 합류 중 상태를 커스텀 프로퍼티로 표시
                //var props = new ExitGames.Client.Photon.Hashtable
                //{
                //    { "merging", true }
                //};
                //PhotonNetwork.CurrentRoom.SetCustomProperties(props);

                Debug.Log("[PUN] 내 방 닫기 완료");
            }
            else
            {
                Debug.LogWarning("[PUN] 방이 없는 상태에서 닫기를 시도함");
            }

            // 다른 방으로 이동하기 위해 우선 현재 방에서 나가기
            if (PhotonNetwork.InRoom)
            {
                //_leavingForMerge = true;
                PhotonNetwork.LeaveRoom(false);
                Debug.Log("[PUN] 내 방 나가기 요청(merge 준비)");
            }

            CancelInvoke(nameof(BroadcastLfg));   // LFG 주기 중단
            //_me = null; // GetMyLfg()가 null을 반환하여 FSM이 무시
        }

        private void OnUpdateLocalPartyAdvertised(List<string> mergedUids, int newSize)
        {
            if (_me == null) return;

            _me.uids = mergedUids;
            _me.size = newSize;

            Debug.Log($"[LFG] 호스트 광고 갱신: 인원 {newSize}/{_me.max}, 멤버: {string.Join(",", mergedUids)}");

            foreach (var p in PhotonNetwork.CurrentRoom.Players)
            {
                Debug.Log($"[LFG] 실제 포톤의 방 인원: {p.Value.UserId}");
            }
            Debug.Log($"[LFG] 실제 포톤의 방 총원: {PhotonNetwork.CurrentRoom.PlayerCount}");

            // 정원이 덜 찼으면, 다음 합류를 받도록 즉시 한 번 더 LFG 발행
            if (newSize < _me.max)
            {
                BroadcastLfg();
            }
            else
            {
                // TODO: 정원 딱 맞으면 투표 오픈 트리거 등
                Debug.Log("[LFG] 정원 달성, 투표 시작.");
            }
        }


        public override void OnLeftRoom() => _leftRoom = true;
        public override void OnConnectedToMaster() => _connectedToMaster = true;
        public override void OnJoinedLobby() => _joinedLobby = true;
        public override void OnPlayerEnteredRoom(Player newPlayer) => RenewAdvertisement();
        public override void OnPlayerLeftRoom(Player otherPlayer) => RenewAdvertisement();
        public override void OnJoinedRoom() => _joinedTargetRoom = true;
        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            _joinFailed = true;
            Debug.LogWarning($"[PUN] JoinRoom 실패: {returnCode} / {message}");
        }


        private void RenewAdvertisement()
        {
            // 실제 방 인원 기반으로 _me 갱신
            if (_me == null || PhotonNetwork.CurrentRoom == null) return;

            List<string> uids = new();
            foreach (var p in PhotonNetwork.CurrentRoom.Players)
            {
                uids.Add(p.Value.UserId);
            }

            _me.uids = uids.ToList();
            _me.size = PhotonNetwork.CurrentRoom.PlayerCount;

            // 인원이 변동되어 광고 업데이트
            BroadcastLfg();
        }

        private MatchMessages.LfgMsg FindLfgByRoom(string room)
        {
            foreach (var m in _cache.Alive(NowMs()))
            {
                if (m.room == room)
                {
                    return m;
                }
            }

            return null;
        }

        private long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
