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
        [Header("Party")]
        [SerializeField] private PartyServices _party;

        [Header("Chat")]
        [SerializeField] private ServerSettings serverSettings;
        [SerializeField] private string _region = "asia";
        [SerializeField] private string _channel = "mm-standard";
        [SerializeField] private int _history = 10;

        [Header("LFG")]
        [SerializeField] private int _lfgTtlMs = 15000;
        [SerializeField] private float _lfgIntervalSec = 3f;

        private Coroutine _joinRetryCO;
        private Coroutine _observeCO;
        private Coroutine _advertisementCO;
        private bool _leftRoom;
        private bool _connectedToMaster;
        private bool _joinedLobby;
        private bool _joinedTargetRoom;
        private bool _joinFailed;
        private string _lastTicketMatchId; // cancel 검증 용

        public ChatService Chat { get; private set; }
        private LfgCache _cache;
        private MatchFSM _fsm;
        private MatchMessages.LfgMsg _me;
        private float _sweepTimer;
        private const float SWEEP_INTERVAL = 10f; // 캐시 청소 주기
        private string _currentPartyId;

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

        public override void OnEnable()
        {
            if (_party != null)
            {
                _party.OnPartyJoinedChannel += HandlePartyJoined;
                _party.OnPartyLeftChannel += HandlePartyLeft;
            }
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

            _fsm.BeginObserve = (matchId, expectedUids, windowEndMs) =>
            {
                StartObserveMerge(matchId, expectedUids, windowEndMs);
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

            if (_party != null)
            {
                _party.OnPartyJoinedChannel -= HandlePartyJoined;
                _party.OnPartyLeftChannel -= HandlePartyLeft;
            }
        }

        private void OnChatConnected()
        {
            // 채널 구독
            Chat.Subscribe(_channel, _history);

            Debug.Log($"[MatchClient] 채팅 서버 연결 성공, 채널 구독: {_channel}");

            Invoke(nameof(StartLfgBroadcastSafely), 0.1f);
        }

        private void StartLfgBroadcastSafely()
        {
            if (_me == null)
            {
                Debug.LogWarning("[MatchClient] 아직 파티 정보(_me)가 없어서 LFG 발행 대기 중...");
                Invoke(nameof(StartLfgBroadcastSafely), 0.2f);
                return;
            }

            Debug.Log($"[MatchClient] LFG 발행 시작 (주기: {_lfgIntervalSec}초)");
            // 첫 LFG 즉시 발행
            BroadcastLfg();
            // 주기 브로드캐스트 시작
            InvokeRepeating(nameof(BroadcastLfg), _lfgIntervalSec, _lfgIntervalSec);
        }

        private void BroadcastLfg()
        {
            if (_me == null)
            {
                Debug.LogError("[MatchClient] _me가 null");
                return;
            }
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning($"[MatchClient] 방에 있지 않음, {PhotonNetwork.NetworkClientState.ToString()}");
                return;
            }

            var room = PhotonNetwork.CurrentRoom;
            if (room == null)
            {
                Debug.Log("[MatchClient] CurrentRoom이 null");
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

            Debug.Log($"[MatchClient] 내가 방({_me.room}) 정보를 발행함. 파티ID:{_me.partyId}, 인원:{_me.size}/{_me.max}");

            foreach (var p in PhotonNetwork.CurrentRoom.Players)
            {
                Debug.Log($"[MatchClient] 현재 방 인원의 uid: {p.Value.UserId}");
            }
            Debug.Log($"[MatchClient] 현재 방 총원: {PhotonNetwork.CurrentRoom.PlayerCount}");

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
                Debug.Log($"[MatchClient] 다른 파티 LFG 수신: 파티ID:{msg.partyId}, 방:{msg.room}, 인원:{msg.size}/{msg.max}");
                _fsm.TryMatchWith(_cache);
            }
            else if (json.Contains("\"t\":\"MATCH_LOCK\""))
            {
                var m = JsonUtility.FromJson<MatchMessages.MatchLockMsg>(json);
                Debug.Log($"[MatchClient] LOCK 메시지 수신 -> 호스트방:{m.hostRoom}, 게스트방:{m.guestRoom}, 매치ID:{m.matchId}");
                _fsm.OnLock(m);
            }
            //else if (json.Contains("\"t\":\"MATCH_ACK\""))
            //{
            //    var a = JsonUtility.FromJson<MatchMessages.MatchAckMsg>(json);
            //    Debug.Log($"[MatchClient] ACK 메시지 수신 -> 매치ID:{a.matchId}, 결과:{a.ok}");
            //    _fsm.OnAck(a);
            //}
        }

        private void OnDM(string sender, object raw)
        {
            var json = raw as string;
            if (string.IsNullOrEmpty(json)) return;

            if (json.Contains("\"t\":\"TICKET\""))
            {
                var t = JsonUtility.FromJson<MatchMessages.TicketMsg>(json);
                Debug.Log($"[MatchClient] 티켓 수신 -> 방:{t.room}, 만료:{t.exp}, 매치ID:{t.matchId}");
                _lastTicketMatchId = t.matchId;
                _fsm.OnTicket(t);
            }
            else if (json.Contains("\"t\":\"MATCH_ACK\""))
            {
                var a = JsonUtility.FromJson<MatchMessages.MatchAckMsg>(json);
                Debug.Log("[MatchClient] ACK 수신");
                _fsm.OnAck(a);
            }
            else if (json.Contains("\"t\":\"MATCH_CANCEL\""))
            {
                var c = JsonUtility.FromJson<MatchMessages.MatchCancelMsg>(json);
                Debug.Log("[MatchClient] MATCH_CANCEL 수신");
                HandleMatchCancel(c);
            }
        }

        // PUN 연동
        private void SetExpectedUsersViaPun(List<string> mergedUids)
        {
            Debug.Log($"[MatchClient] SetExpectedUsers: {string.Join(",", mergedUids)}");
            if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.SetExpectedUsers(mergedUids.ToArray());
            }
            else
            {
                Debug.LogWarning($"[MatchClient] 방에 있지 않습니다");
            }
        }

        private void TryJoinRoomUntilViaPun(string room, long expMs, Action<bool> result)
        {
            if (_joinRetryCO != null)
            {
                StopCoroutine(_joinRetryCO);
                _joinRetryCO = null;
            }
            _joinRetryCO = StartCoroutine(JoinRetry(room, expMs, result));
        }

        private IEnumerator JoinRetry(string room, long expMs, Action<bool> result)
        {
            int attempt = 0;

            Debug.Log("[MatchClient] 자신 Room에서 나올 때 까지 대기");

            if (PhotonNetwork.InRoom)
            {
                Debug.Log("[MatchClient] 현재 방에서 나가는 중...");
                PhotonNetwork.LeaveRoom(false);
            }
            yield return new WaitUntil(() => !PhotonNetwork.InRoom);
            Debug.Log("[MatchClient] 현재 방에서 나옴");

            // 로비 진입까지 대기
            _joinedLobby = PhotonNetwork.NetworkClientState == ClientState.JoinedLobby;

            Debug.Log("[MatchClient] 로비에 진입할 때 까지 대기");
            if (!_joinedLobby)
            {
                Debug.Log("[MatchClient] 로비 진입 대기...");
                PhotonNetwork.JoinLobby();
                yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.JoinedLobby);
            }

            Debug.Log($"[MatchClient] 현재 클라이언트 상태: {PhotonNetwork.NetworkClientState.ToString()}");

            // 유효시간 동안 Join 재시도
            while (NowMs() < expMs)
            {
                // 이미 목표 방에 있으면 종료
                if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.Name == room)
                {
                    Debug.Log("[MatchClient] 이미 목표 방에 입장되어 있음. 재시도 종료.");
                    yield break;
                }

                _joinedTargetRoom = false;
                _joinFailed = false;

                // 로비에 없다면 먼저 로비 진입
                if (!(PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer ||
                      PhotonNetwork.NetworkClientState == ClientState.JoinedLobby))
                {
                    Debug.Log("[MatchClient] 매치메이킹 가능한 상태가 아님. 로비 진입 대기");
                    yield return new WaitUntil(() =>
                        PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer ||
                        PhotonNetwork.NetworkClientState == ClientState.JoinedLobby
                    );
                }

                attempt++;
                Debug.Log($"[MatchClient] 방 합류 시도 {attempt}회 -> {room} (state:{PhotonNetwork.NetworkClientState})");
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
                    Debug.Log("[MatchClient] 방 합류 성공, 코루틴 종료");
                    result?.Invoke(true);
                    yield break;
                }

                // 실패했으면 백오프 후 다음 시도
                Debug.Log("[MatchClient] 방 합류 실패 또는 타임아웃. 재시도 대기...");
                yield return new WaitForSeconds(Mathf.Min(0.3f * attempt, 1.5f));
            }

            result?.Invoke(false);
            Debug.LogWarning("[MatchClient] 티켓 만료: 더 이상 방 합류를 시도하지 않음");
        }

        private void CloseMyRoomForMergeViaPun()
        {
            Debug.Log("[MatchClient] CloseMyRoomForMerge: prevent new entries");

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

                Debug.Log("[MatchClient] 내 방 닫기 완료");
            }
            else
            {
                Debug.LogWarning("[MatchClient] 방이 없는 상태에서 닫기를 시도함");
            }

            // 다른 방으로 이동하기 위해 우선 현재 방에서 나가기
            if (PhotonNetwork.InRoom)
            {
                //_leavingForMerge = true;
                PhotonNetwork.LeaveRoom(false);
                Debug.Log("[MatchClient] 내 방 나가기 요청(merge 준비)");
            }

            CancelInvoke(nameof(BroadcastLfg));   // LFG 주기 중단
            //_me = null; // GetMyLfg()가 null을 반환하여 FSM이 무시
        }

        private void OnUpdateLocalPartyAdvertised(List<string> mergedUids, int newSize)
        {
            if (_me == null) return;

            _me.uids = mergedUids;
            _me.size = newSize;

            Debug.Log($"[MatchClient] 호스트 광고 갱신: 인원 {newSize}/{_me.max}, 멤버: {string.Join(",", mergedUids)}");

            foreach (var p in PhotonNetwork.CurrentRoom.Players)
            {
                Debug.Log($"[MatchClient] 실제 포톤의 방 인원: {p.Value.UserId}");
            }
            Debug.Log($"[MatchClient] 실제 포톤의 방 총원: {PhotonNetwork.CurrentRoom.PlayerCount}");

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

        private void StartObserveMerge(string matchId, List<string> expectedUids, long windowEndMs)
        {
            if (_observeCO != null)
            {
                StopCoroutine(_observeCO);
                _observeCO = null;
            }
            _observeCO = StartCoroutine(ObserveMerge(matchId, expectedUids, windowEndMs));
        }

        private void StopObserveMerge()
        {
            if (_observeCO != null)
            {
                StopCoroutine(_observeCO);
                _observeCO = null;
            }
        }

        // 호스트가 게스트에게
        private IEnumerator ObserveMerge(string matchId, List<string> expectedUids, long windowEndMs)
        {
            // 호스트와 게스트 분리
            var hostSet = new HashSet<string>(_me?.uids ?? new List<string>(), StringComparer.Ordinal);
            var guestUids = expectedUids.Where(uid => !hostSet.Contains(uid)).ToList();

            while (NowMs() < windowEndMs)
            {
                var joined = GetCurrentRoomUids(); // 실제 내 방에 들어온 uid들
                bool allIn = expectedUids.All(joined.Contains);
                if (allIn) // 전원 합류하여 종료
                {
                    yield break;
                }

                yield return null;
            }

            // 시간 만료까지 못들어온 경우 취소
            Debug.Log("[MatchClient] 관찰 만료, 일부 미합류로 MATCH_CANCEL 발신");

            var cancel = new MatchMessages.MatchCancelMsg
            {
                matchId = matchId,
                reason = "timeout",
                uids = guestUids
            };

            foreach (var uid in guestUids)
            {
                Chat.SendDM(uid, cancel);
            }

            // 방 오픈
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
            {
                PhotonNetwork.CurrentRoom.IsOpen = true;
                PhotonNetwork.CurrentRoom.IsVisible = true;
                PhotonNetwork.CurrentRoom.SetExpectedUsers(Array.Empty<string>());

                Debug.Log("[MatchClient] 호스트 방 재오픈");
            }
            else
            {
                Debug.LogWarning("[MatchClient] 방에 없거나 방이 없습니다");
            }

            RenewAdvertisement();
            BroadcastLfg();

            StopObserveMerge();
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
            Debug.LogWarning($"[MatchClient] JoinRoom 실패: {returnCode} / {message}");
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

        private HashSet<string> GetCurrentRoomUids()
        {
            HashSet<string> set = new(StringComparer.Ordinal);
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom?.Players != null)
            {
                foreach (var p in PhotonNetwork.CurrentRoom.Players)
                {
                    var uid = p.Value.UserId;
                    if (!string.IsNullOrEmpty(uid))
                    {
                        set.Add(uid);
                    }
                }
            }

            return set;
        }

        // 게스트용
        private void HandleMatchCancel(MatchMessages.MatchCancelMsg c)
        {
            // 오래된 신호 방지
            if (!string.IsNullOrEmpty(_lastTicketMatchId) && c.matchId != _lastTicketMatchId)
            {
                Debug.Log($"[MatchClient] 오래된 matchId 무시: {c.matchId} != {_lastTicketMatchId}");
                return;
            }

            // 내가 대상이 아니면 return
            if (c.uids != null && !c.uids.Contains(_fsm.MyUid))
            {
                return;
            }

            // 진행 중인 참가 있다면 시도 중지
            if (_joinRetryCO != null)
            {
                StopCoroutine(_joinRetryCO);
                _joinRetryCO = null;
            }

            // FSM Idle로 복귀
            _fsm.OnMatchCancelFromHost(c.matchId);
            _lastTicketMatchId = null;

            // 내 방 확보 및 광고
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
            {
                // 이미 어떤 방에 있다면, 재오픈 후 바로 광고
                PhotonNetwork.CurrentRoom.IsOpen = true;
                PhotonNetwork.CurrentRoom.IsVisible = true;
                Debug.Log("[MatchClient] CANCEL 처리: 현재 방 재오픈");
                RenewAdvertisement();
                BroadcastLfg();
            }
            else
            {
                // 방이 없다면 새 파티 방을 만들고 참가 완료 후 광고
                var roomName = $"p_{_me.partyId}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                var opt = new RoomOptions
                {
                    MaxPlayers = (byte)_me.max,
                    IsOpen = true,
                    IsVisible = true
                };

                // 조인 완료 후 광고를 보장을 위해 대기
                PhotonNetwork.JoinOrCreateRoom(roomName, opt, TypedLobby.Default);

                if (_advertisementCO != null)
                {
                    StopCoroutine(_advertisementCO);
                    _advertisementCO = null;
                }
                _advertisementCO = StartCoroutine(AdvertiseWhenJoined());
            }
        }

        private void HandlePartyJoined(string partyId)
        {
            // 파티ID 갱신
            if (_me != null)
            {
                _me.partyId = _currentPartyId;
            }

            // 방이 없으면 파티 전용 방 하나 보장
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            {
                string myUid = _fsm.MyUid ?? Guid.NewGuid().ToString("N");
                string roomName = $"p_{partyId}_{myUid.Substring(0, 6)}";
                var opt = new RoomOptions
                {
                    MaxPlayers = (byte)(_me?.max > 0 ? _me.max : 4), // 최대 인원 4로 하드코딩 됨
                    IsOpen = true,
                    IsVisible = true
                };
                PhotonNetwork.JoinOrCreateRoom(roomName, opt, TypedLobby.Default);

                // 방 들어간 뒤 광고 보장
                if (_advertisementCO != null)
                {
                    StopCoroutine(_advertisementCO);
                    _advertisementCO = null;
                }
                _advertisementCO = StartCoroutine(AdvertiseWhenJoined());

                return;
            }

            // 이미 방이 있으면 LFG 최신화 후 즉시 광고
            RefreshMeFromPhoton();
            BroadcastLfg();
        }

        private void HandlePartyLeft(string partyId)
        {
            if (_currentPartyId != partyId) return;

            _currentPartyId = null;
            if (_me != null) _me.partyId = null; // 같은 파티 회피 로직을 타지 않도록

            // 방은 유지, LFG 갱신
            RefreshMeFromPhoton();
            BroadcastLfg();
        }

        private IEnumerator AdvertiseWhenJoined()
        {
            yield return new WaitUntil(() => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null);
            RenewAdvertisement();
            BroadcastLfg();
        }

        private void RefreshMeFromPhoton()
        {
            if (_me == null)
            {
                return;
            }
            var room = PhotonNetwork.CurrentRoom;
            if (room == null)
            {
                return;
            }

            _me.room = room.Name;
            _me.uids.Clear();
            if (room.Players != null)
            {
                foreach (var kv in room.Players)
                {
                    var uid = kv.Value?.UserId;
                    if (!string.IsNullOrEmpty(uid))
                    {
                        _me.uids.Add(uid);
                    }
                }
            }
            _me.size = _me.uids.Count;
            _me.max = room.MaxPlayers;
            _me.ts = NowMs();

            // 지금은 roomCreatedAt을 최초 SetLocalPartyRoom을 사용하고 있음
        }
    }
}
