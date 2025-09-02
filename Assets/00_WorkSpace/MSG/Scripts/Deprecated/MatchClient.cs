using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace MSG.Deprecated
{
    public class MatchClient : MonoBehaviourPunCallbacks
    {
        #region Fields and Properties

        [Header("Party")]
        [SerializeField] private PartyServices _party;

        [Header("Chat")]
        [SerializeField] private ServerSettings serverSettings;
        [SerializeField] private string _region = "asia";
        [SerializeField] private int _history = 10;

        [Header("LFG")]
        [SerializeField] private int _lfgTtlMs = 15000;
        [SerializeField] private float _lfgIntervalSec = 3f;

        private Coroutine _joinRetryCO;
        private Coroutine _observeCO;
        private Coroutine _advertisementCO;
        private Coroutine _recallCO;
        private Coroutine _returnHomeCO;
        private bool _leftRoom;
        private bool _connectedToMaster;
        private bool _joinedLobby;
        private bool _joinedTargetRoom;
        private bool _joinFailed;
        private string _lastTicketMatchId; // cancel 검증 용
        private bool _isMatching = false;

        public ChatService Chat { get; private set; }
        private LfgCache _cache;
        private MatchFSM _fsm;
        private MatchMessages.LfgMsg _me;
        private float _sweepTimer;
        private string _currentPartyId;
        private List<string> _lastExpectedUids = new(); // 아직 매치에는 못들어왔지만 들어올 것으로 예상되는 uid


        private const float SWEEP_INTERVAL = 10f; // 캐시 청소 주기
        private const long RECALL_EXP = 5000L;   // 리콜 DM 만료 시간
        private const string Channel = "mm-standard";
        private const string ROOM_PROP_CREATED_AT = "roomCreatedAt";
        private const string ROOM_PROP_ANCHOR_UID = "anchorUid";
        private const int MAX_PLAYERS = 4;

        #endregion


        #region Public Methods

        // 초기 설정
        public void SetLocalPartyRoom(string partyId, string room, string contactUid, string anchorUid, List<string> uids, int size, int max, long roomCreatedAt)
        {
            var now = NowMs();
            _me = new MatchMessages.LfgMsg
            {
                id = Guid.NewGuid().ToString("N"),
                partyId = partyId,
                room = room,
                contactUid = contactUid,
                anchorUid = anchorUid,
                uids = uids,
                size = size,
                max = max,
                ts = now,
                expiresAt = now + _lfgTtlMs,
                roomCreatedAt = roomCreatedAt
            };
        }

        #endregion


        #region Unity Methods

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
                ChannelName = Channel,
                MyUid = FirebaseManager.Instance.Auth.CurrentUser.UserId,
                GetMyLfg = () => _me,

                // PUN 연동 콜백 주입
                SetExpectedUsers = SetExpectedUsersViaPun,
                TryJoinRoomUntil = TryJoinRoomUntilViaPun,
                CloseMyRoomForMerge = CloseMyRoomForMergeViaPun,
                UpdateLocalPartyAdvertised = OnUpdateLocalPartyAdvertised
            };

            _fsm.BeginObserve = (matchId, expectedUids, windowEndMs) =>
            {
                _lastExpectedUids = expectedUids?.ToList();

                if (_observeCO != null)
                {
                    StopCoroutine(_observeCO);
                    _observeCO = null;
                }
                _observeCO = StartCoroutine(ObserveMerge(matchId, expectedUids, windowEndMs));
            };

            _fsm.OnGuestTicketTimeout = ReopenMyRoomAfterGuestTicketTimeout;

            Chat = new ChatService();
            Chat.Connected += OnChatConnected;
            Chat.OnChannelMessage(Channel, OnMatchChannel);
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
            Chat?.Unsubscribe(Channel);

            if (_party != null)
            {
                _party.OnPartyJoinedChannel -= HandlePartyJoined;
                _party.OnPartyLeftChannel -= HandlePartyLeft;
            }
        }

        #endregion


        #region Button Methods

        // 매칭 시작 버튼
        public void OnClickStartMatchingButton()
        {
            if (_isMatching) return;
            if (!string.IsNullOrEmpty(_party.CurrentPartyId) && !_party.IsLeader)
            {
                Debug.Log("[MatchClient] 파티원은 시작 불가, 리더만 가능합니다. 준비 상태로 간주하고 return");
                return;
            }

            // 방, 데이터 준비
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            {
                // 파티가 있다면 파티 방, 없다면 솔로 방을 생성하여 진입
                string partyId = _currentPartyId ?? $"solo:{(_fsm?.MyUid ?? Guid.NewGuid().ToString("N"))}";
                string myUid = _fsm?.MyUid ?? Guid.NewGuid().ToString("N");
                string roomName = $"p_{partyId}_{myUid.Substring(0, 6)}";

                var opt = new RoomOptions
                {
                    MaxPlayers = _me?.max > 0 ? _me.max : MAX_PLAYERS,
                    IsOpen = true,
                    IsVisible = true,
                    PublishUserId = true
                };

                PhotonNetwork.JoinOrCreateRoom(roomName, opt, TypedLobby.Default);
                StartCoroutine(StartAfterJoined()); // 조인 완료 후 시작
                return;
            }

            // 이미 방이 있으면 바로 시작
            StartMatchingInCurrentRoom();
        }

        // 매칭 중단 버튼
        public void OnClickStopMatchingButton()
        {
            if (!_isMatching) return;
            _isMatching = false;

            // LFG 주기 중단
            CancelInvoke(nameof(BroadcastLfg));

            if (_observeCO != null)
            {
                StopCoroutine(_observeCO);
                _observeCO = null;
            }
            if (_joinRetryCO != null)
            {
                StopCoroutine(_joinRetryCO);
                _joinRetryCO = null;
            }

            _fsm.DisableMatching();
            _cache?.Sweep(NowMs()); // 캐시에 남아있는 LFG 때문에 매치 시작되는 것을 방지

            bool amPartyLeader = _party.IsLeader; // 내가 파티 리더인가
            bool amMaster = PhotonNetwork.IsMasterClient;   // 내가 마스터 클라이언트인가

            // 호스트가 중지를 눌렀을 때, 파티원 외의 사람들에게 나가라는 메시지 발신
            NotifyMatchStopToAll("host_stop");

            // 파티원이 누른 경우 리더에게 DM
            if (!string.IsNullOrEmpty(_party.CurrentPartyId) && !amPartyLeader)
            {
                var leader = _party.LeaderUid;
                if (!string.IsNullOrEmpty(leader))
                {
                    Chat.SendDM(leader, new MatchMessages.PartyCancelMsg
                    {
                        t = "PARTY_CANCEL",
                        partyId = _party.CurrentPartyId,
                        senderUid = _fsm.MyUid,
                        reason = "member_stop"
                    });
                }

                if (_returnHomeCO != null)
                {
                    StopCoroutine(_returnHomeCO);
                    _returnHomeCO = null;
                }
                _returnHomeCO = StartCoroutine(ReturnToHomeRoom()); // 복귀
                Debug.Log("[MatchClient] 파티원 취소: 리더에 통보 후 홈룸으로 복귀");
                return;
            }


            // 1. 내가 파티장이면서 동시에 마스터 클라이언트일 때
            if (amPartyLeader && amMaster && IsInHomeRoom())
            {
                // 파티원 아닌 사람 정리
                var partySet = new HashSet<string>(_party?.Members);
                foreach (var p in PhotonNetwork.CurrentRoom.Players.Values)
                {
                    if (!partySet.Contains(p.UserId))
                    {
                        // 나가라는 메시지 전송
                        Chat.SendDM(p.UserId, new MatchMessages.MatchCancelMsg
                        {
                            matchId = _lastTicketMatchId,
                            reason = "onclick_stop_match",
                            uids = new List<string> { p.UserId }
                        });
                    }
                }

                PhotonNetwork.CurrentRoom.IsOpen = true;
                if (partySet.Count > 0)
                {
                    PhotonNetwork.CurrentRoom.SetExpectedUsers(partySet.ToArray());
                }
                else
                {
                    PhotonNetwork.CurrentRoom.ClearExpectedUsers();
                }

                Debug.Log("[MatchClient] 파티만 남기고 방 유지.");
                return;
            }

            // 2. 내가 파티장이면서 동시에 마스터가 아니거나, 솔로(마스터 여부와 관계 없이)일 때
            if (_returnHomeCO != null)
            {
                StopCoroutine(_returnHomeCO);
                _returnHomeCO = null;
            }
            _returnHomeCO = StartCoroutine(ReturnToHomeRoom()); // 내 방으로 복귀

            // 3. 2번 상황 중 자신이 파티장이면 파티원 부르기
            if (amPartyLeader)
            {
                string home = CalculateHomeRoomName();
                foreach (var uid in (_party?.Members ?? new HashSet<string>()))
                {
                    if (uid == _fsm.MyUid)
                    {
                        continue;
                    }
                    Chat.SendDM(uid, new MatchMessages.PartyRecallMsg
                    {
                        t = "PARTY_RECALL",
                        room = home,
                        partyId = _party.CurrentPartyId,
                        leaderUid = FirebaseManager.Instance.Auth.CurrentUser.UserId,
                        exp = NowMs() + RECALL_EXP
                    });
                }
            }

            Debug.Log("[MatchClient] 매칭 중지, 내 방으로 복귀");
        }

        #endregion


        #region Photon Callbacks

        public override void OnLeftRoom() => _leftRoom = true;
        public override void OnConnectedToMaster() => _connectedToMaster = true;
        public override void OnJoinedLobby() => _joinedLobby = true;
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            EnsureMeFromPhoton();

            if (PhotonNetwork.IsMasterClient)
            {
                var room = PhotonNetwork.CurrentRoom;
                var anchor = room?.CustomProperties?[ROOM_PROP_ANCHOR_UID] as string;
                bool anchorPresent = room?.Players?.Values?.Any(p => p?.UserId == anchor) ?? false;
                if (string.IsNullOrEmpty(anchor) || !anchorPresent)
                {
                    AnchorRoomToMyself();
                }
                if (_isMatching)
                {
                    BroadcastLfg();
                }
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            EnsureMeFromPhoton();

            if (PhotonNetwork.IsMasterClient)
            {
                var room = PhotonNetwork.CurrentRoom;
                var anchor = room?.CustomProperties?[ROOM_PROP_ANCHOR_UID] as string;
                bool anchorPresent = room?.Players?.Values?.Any(p => p?.UserId == anchor) ?? false;
                if (!anchorPresent) // 기존 앵커가 나갔다면
                {
                    AnchorRoomToMyself();
                }
                if (_isMatching)
                {
                    BroadcastLfg();
                }
            }
        }
        public override void OnJoinedRoom() => _joinedTargetRoom = true;
        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            _joinFailed = true;
            Debug.LogWarning($"[MatchClient] JoinRoom 실패: {returnCode} / {message}");
        }
        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (_me != null)
            {
                _me.contactUid = newMasterClient?.UserId ?? _me.contactUid;
                Debug.Log($"[MatchClient] 마스터 변경, contactUid: {_me.contactUid}");
            }

            EnsureMeFromPhoton();

            // 앵커가 없거나 방에 없는 경우, 새 마스터가 앵커 승계
            if (PhotonNetwork.IsMasterClient)
            {
                var room = PhotonNetwork.CurrentRoom;
                var anchor = room?.CustomProperties?[ROOM_PROP_ANCHOR_UID] as string;
                bool anchorPresent = room?.Players?.Values?.Any(p => p?.UserId == anchor) ?? false;

                if (string.IsNullOrEmpty(anchor) || !anchorPresent)
                {
                    AnchorRoomToMyself();
                }
                // 매칭 중이면, 호스트만 LFG 재발행
                if (_isMatching)
                {
                    BroadcastLfg();
                    CancelInvoke(nameof(BroadcastLfg));
                    InvokeRepeating(nameof(BroadcastLfg), _lfgIntervalSec, _lfgIntervalSec);

                    Debug.Log($"[MatchClient] LFG 발행 시작 (주기: {_lfgIntervalSec}초)");
                }
            }
        }

        #endregion

        #region Private Methods

        private long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private void EnsureChatSubscribed()
        {
            if (Chat == null)
            {
                Debug.LogError("[MatchClient] Chat이 null");
                return;
            }

            // 구독이 안될까봐 다시 호출
            Chat.Subscribe(Channel, _history);
        }

        private void OnChatConnected()
        {
            // 채널 구독
            Chat.Subscribe(Channel, _history);
            Debug.Log($"[MatchClient] 채팅 서버 연결 성공, 채널 구독: {Channel}");
            //Invoke(nameof(StartLfgBroadcastSafely), 0.1f);
        }

        // 즉시 Broadcast 하지 않아 이제 사용 안할 듯
        //private void StartLfgBroadcastSafely()
        //{
        //    if (_me == null)
        //    {
        //        Debug.LogWarning("[MatchClient] 아직 파티 정보(_me)가 없어서 LFG 발행 대기 중...");
        //        Invoke(nameof(StartLfgBroadcastSafely), 0.2f);
        //        return;
        //    }

        //    Debug.Log($"[MatchClient] LFG 발행 시작 (주기: {_lfgIntervalSec}초)");
        //    // 첫 LFG 즉시 발행
        //    BroadcastLfg();
        //    // 주기 브로드캐스트 시작
        //    InvokeRepeating(nameof(BroadcastLfg), _lfgIntervalSec, _lfgIntervalSec);
        //}

        private void BroadcastLfg()
        {
            if (!_isMatching) return;

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
                contactUid = _me.contactUid,
                anchorUid = _me.anchorUid,
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

            Chat.Publish(Channel, msg);
        }

        private void OnMatchChannel(string sender, object raw)
        {
            if (!_isMatching) return;

            var json = raw as string;
            if (string.IsNullOrEmpty(json)) return;

            if (json.Contains("\"t\":\"LFG\""))
            {
                if (!PhotonNetwork.IsMasterClient) return; // LFG 발행은 마스터만
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
                if (!_isMatching)
                {
                    Debug.Log("[MatchClient] 티켓 수신 받았으나 매칭 취소되어 무시");
                    return;
                }

                var t = JsonUtility.FromJson<MatchMessages.TicketMsg>(json);
                Debug.Log($"[MatchClient] 티켓 수신 -> 방:{t.room}, 만료:{t.exp}, 매치ID:{t.matchId}");
                _lastTicketMatchId = t.matchId;
                _fsm.OnTicket(t);
            }
            else if (json.Contains("\"t\":\"MATCH_ACK\""))
            {
                if (!_isMatching)
                {
                    Debug.Log("[MatchClient] ACK 수신 받았으나 매칭 취소되어 무시");
                    return;
                }

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
            else if (json.Contains("\"t\":\"PARTY_RECALL\""))
            {
                var r = JsonUtility.FromJson<MatchMessages.PartyRecallMsg>(json);
                if (r.partyId == _party.CurrentPartyId && NowMs() <= r.exp)
                {
                    // 내 현재 매칭 여부와 무관하게 리더 방으로 이동
                    if (_recallCO != null)
                    {
                        StopCoroutine(_recallCO);
                        _recallCO = null;
                    }
                    _recallCO = StartCoroutine(ReturnToSpecificRoom(r.room));
                }
            }
            else if (json.Contains("\"t\":\"PARTY_CANCEL\""))
            {
                var m = JsonUtility.FromJson<MatchMessages.PartyCancelMsg>(json);
                if (_party.IsLeader && m.partyId == _party.CurrentPartyId)
                {
                    Debug.Log($"[MatchClient] PARTY_CANCEL from {m.senderUid}, reason={m.reason}");

                    // 리더가 파티 매칭 중지
                    if (_isMatching) OnClickStopMatchingButton();

                    // 모두 홈룸으로 모이도록 리콜
                    string home = CalculateHomeRoomName();
                    var recall = new MatchMessages.PartyRecallMsg
                    {
                        room = home,
                        partyId = _party.CurrentPartyId,
                        leaderUid = _fsm.MyUid,
                        exp = NowMs() + RECALL_EXP,
                    };
                    foreach (var uid in _party.Members)
                    {
                        if (uid != _fsm.MyUid) Chat.SendDM(uid, recall);
                    }
                }
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

            // 마스터 진입까지 대기
            _connectedToMaster = PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer;

            Debug.Log("[MatchClient] 마스터에 진입할 때 까지 대기");
            if (!_connectedToMaster)
            {
                Debug.Log("[MatchClient] 마스터 진입 대기...");

                yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer);

                // TODO: 여기서 계속 기다리면 무한 대기 가능성 있음 추가처리 필요
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

                // 마스터에 없다면 먼저 로비 진입
                if (!(PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer))
                {
                    Debug.Log("[MatchClient] 매치메이킹 가능한 상태가 아님. 로비 진입 대기");
                    yield return new WaitUntil(() =>
                        PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer
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

            // 새로운 입장 금지
            if (PhotonNetwork.CurrentRoom != null)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;

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

            // 방 이동 및 먼저 나가기는 JoinRetry에서 시도하는 것으로 이관
            // 다른 방으로 이동하기 위해 우선 현재 방에서 나가기
            //if (PhotonNetwork.InRoom)
            //{
            //    //_leavingForMerge = true;
            //    PhotonNetwork.LeaveRoom(false);
            //    Debug.Log("[MatchClient] 내 방 나가기 요청(merge 준비)");
            //}

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

        // 호스트가 게스트에게
        private IEnumerator ObserveMerge(string matchId, List<string> expectedUids, long windowEndMs)
        {
            // 호스트와 게스트 분리
            var hostSet = new HashSet<string>(_me?.uids ?? new List<string>());
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
                PhotonNetwork.CurrentRoom.SetExpectedUsers(GetCurrentRoomUids().ToArray());

                Debug.Log("[MatchClient] 호스트 방 재오픈 및 ExpectedUsers 재설정");
            }
            else
            {
                Debug.LogWarning("[MatchClient] 방에 없거나 방이 없습니다");
            }

            RenewAdvertisement();
            BroadcastLfg();
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
            _me.roomCreatedAt = (long)PhotonNetwork.CurrentRoom.CustomProperties[ROOM_PROP_CREATED_AT];

            // 인원이 변동되어 광고 업데이트
            BroadcastLfg();
        }

        //private MatchMessages.LfgMsg FindLfgByRoom(string room)
        //{
        //    Debug.Log($"[MatchClient] FindLfgByRoom 시작");

        //    foreach (var m in _cache.Alive(NowMs()))
        //    {
        //        if (m.room == room)
        //        {
        //            Debug.Log($"[MatchClient] 받은 메시지와 캐시가 일치하여 반환 id: {m.id}, room: {m.room}");
        //            return m;
        //        }
        //        else
        //        {
        //            Debug.Log($"[MatchClient] 받은 메시지와 캐시가 일치하지 않아 반환하지 않음 {m.id}, room: {m.room}");
        //        }
        //    }

        //    Debug.Log($"[MatchClient] FindLfgByRoom 종료. 시작 직후 종료되면 캐시가 null");
        //    return null;
        //}

        private HashSet<string> GetCurrentRoomUids()
        {
            HashSet<string> set = new();
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

            // FSM Finding으로 복귀
            _fsm.OnMatchCancelFromHost(c.matchId);
            _lastTicketMatchId = null;

            var inRoom = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null;
            var inMyHome = inRoom && IsInHomeRoom();
            var iAmMaster = inRoom && PhotonNetwork.IsMasterClient;

            if (!inRoom || !inMyHome || !iAmMaster) // 방에 없고 홈룸이 아니고 마스터가 아니면
            {
                // 홈룸으로 복귀
                if (_returnHomeCO != null)
                {
                    StopCoroutine(_returnHomeCO);
                    _returnHomeCO = null;
                }
                _returnHomeCO = StartCoroutine(ReturnToHomeRoom());
            }

            // 내 홈룸의 마스터라면 재오픈 및 광고
            PhotonNetwork.CurrentRoom.IsOpen = true;

            var uids = GetCurrentRoomUids().ToArray();
            if (uids.Length > 0)
            {
                PhotonNetwork.CurrentRoom.SetExpectedUsers(uids);
            }
            else
            {
                PhotonNetwork.CurrentRoom.ClearExpectedUsers();
            }

            RenewAdvertisement();
            if (_isMatching)
            {
                BroadcastLfg();
            }
        }

        private void HandlePartyJoined(string partyId)
        {
            // 파티ID 갱신
            _currentPartyId = partyId;
            if (_me != null)
            {
                _me.partyId = partyId; // 최신 파티ID 반영
            }

            // 방이 없으면 파티 전용 방 하나 보장
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            {
                string myUid = _fsm?.MyUid ?? Guid.NewGuid().ToString("N");
                string roomName = $"p_{partyId}_{myUid.Substring(0, 6)}";
                var opt = new RoomOptions
                {
                    MaxPlayers = _me?.max > 0 ? _me.max : MAX_PLAYERS,
                    IsOpen = true,
                    IsVisible = true,
                    PublishUserId = true
                };

                PhotonNetwork.JoinOrCreateRoom(roomName, opt, TypedLobby.Default);

                // 방 들어간 뒤 _me 생성/동기화를 보장하기 위해 코루틴 사용
                if (_advertisementCO != null)
                {
                    StopCoroutine(_advertisementCO);
                    _advertisementCO = null;
                }
                _advertisementCO = StartCoroutine(AdvertiseWhenJoined());
                return;
            }

            // 이미 방이 있으면 _me 생성 및 동기화 보장
            EnsureMeFromPhoton();

            // 매칭 중일 때만 광고
            if (_isMatching)
            {
                BroadcastLfg();
            }
        }

        private void HandlePartyLeft(string partyId)
        {
            if (_currentPartyId != partyId) return;

            // 파티 id 초기화
            _currentPartyId = null;
            if (_me != null)
            {
                _me.partyId = null; // 같은 파티 회피 로직 방지
            }

            // 방은 유지하고 현재 방 상태로 _me 보장
            EnsureMeFromPhoton();

            // 매칭 중일 때만 광고
            if (_isMatching)
            {
                BroadcastLfg();
            }
        }

        private IEnumerator AdvertiseWhenJoined()
        {
            yield return new WaitUntil(() => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null);
            EnsureMeFromPhoton();
            // 매칭 중일 때만 광고
            if (_isMatching)
            {
                BroadcastLfg();
            }
        }

        private void EnsureMeFromPhoton()
        {
            var room = PhotonNetwork.CurrentRoom;
            if (room == null) return;

            // 방 커스텀 프로퍼티 설정
            var now = NowMs();
            EnsureRoomAnchorPropsIfNeeded(room, now);

            // 현재 방 인원 수집
            List<string> uids = new();
            if (room.Players != null)
            {
                foreach (var kv in room.Players)
                {
                    var uid = kv.Value?.UserId;
                    if (!string.IsNullOrEmpty(uid)) uids.Add(uid);
                }
            }

            // 커스텀 프로퍼티에서 고정값 읽기
            long createdAt = now;
            if (room.CustomProperties != null && room.CustomProperties.ContainsKey(ROOM_PROP_CREATED_AT))
            {
                createdAt = Convert.ToInt64(room.CustomProperties[ROOM_PROP_CREATED_AT]);
            }

            string anchorUid = _fsm?.MyUid ?? "unknown";
            if (room.CustomProperties != null && room.CustomProperties.ContainsKey(ROOM_PROP_ANCHOR_UID))
            {
                anchorUid = room.CustomProperties[ROOM_PROP_ANCHOR_UID] as string ?? anchorUid;
            }

            // contactUid를 현재 마스터로 설정
            string contactUid = PhotonNetwork.MasterClient?.UserId ?? _fsm?.MyUid ?? "unknown";

            if (_me == null)
            {
                _me = new MatchMessages.LfgMsg
                {
                    id = Guid.NewGuid().ToString("N"),
                    partyId = GetPartyIdSafely(),
                    room = room.Name,
                    contactUid = contactUid,
                    anchorUid = anchorUid,
                    uids = uids.ToList(),
                    size = uids.Count,
                    max = room.MaxPlayers,
                    ts = now,
                    expiresAt = now + _lfgTtlMs,
                    roomCreatedAt = createdAt
                };
            }
            else
            {
                _me.room = room.Name;
                _me.uids = uids.ToList();
                _me.size = uids.Count;
                _me.max = room.MaxPlayers;
                _me.partyId = GetPartyIdSafely();
                _me.contactUid = contactUid;
                _me.ts = now;
            }
        }

        private IEnumerator StartAfterJoined()
        {
            yield return new WaitUntil(() => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null);

            // 방에 들어온 후 _me 동기화
            EnsureMeFromPhoton();

            // 파티가 있다면 ExpectedUsers를 파티 멤버로 세팅하여 재합류
            if (_currentPartyId != null && PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.SetExpectedUsers(_me?.uids?.ToArray() ?? Array.Empty<string>());
                Debug.Log("[MatchClient] Start: 조인 직후 ExpectedUsers(파티) 세팅");
            }

            StartMatchingInCurrentRoom();
        }

        private void StartMatchingInCurrentRoom()
        {
            if (_me == null)
            {
                EnsureMeFromPhoton();
            }
            if (_me == null)
            {
                Debug.LogError("[MatchClient] _me가 준비되지 않아 매칭 시작 불가");
                return;
            }

            _isMatching = true;
            _fsm.EnableMatching();
            EnsureChatSubscribed();
            BroadcastLfg(); // 첫 발행
            InvokeRepeating(nameof(BroadcastLfg), _lfgIntervalSec, _lfgIntervalSec); // 주기 발행

            Debug.Log($"[MatchClient] LFG 발행 시작 (주기: {_lfgIntervalSec}초)");
            Debug.Log("[MatchClient] 매칭 시작");
        }

        private string GetPartyIdSafely()
        {
            var uid = _fsm?.MyUid ?? "unknown";
            return _currentPartyId ?? $"solo:{uid}";
        }

        private void ReopenMyRoomAfterGuestTicketTimeout()
        {
            // 닫아둔 방 오픈
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
            {
                PhotonNetwork.CurrentRoom.IsOpen = true;

                // ExpectedUsers 재설정
                PhotonNetwork.CurrentRoom.SetExpectedUsers(GetCurrentRoomUids().ToArray());
                Debug.Log("[MatchClient] 게스트 측 티켓 미수신. 방 오픈 및 ExpectedUsers 재설정");

                // 매칭 모드라면 광고 재개
                if (_isMatching)
                {
                    RenewAdvertisement();
                    BroadcastLfg();
                }
            }
            else
            {
                Debug.LogWarning("[MatchClient] (게스트) 티켓 미수신 복구 시 방에 없음필요 시 파티 방 재생성 고려");
                // 정책에 따라 여기서 새 방 생성/조인도 가능
            }
        }

        // 방 커스텀 프로퍼티 보장, 마스터가 최초 1회만 세팅
        private void EnsureRoomAnchorPropsIfNeeded(Room room, long nowMs)
        {
            if (room == null) return;
            var props = room.CustomProperties ?? new ExitGames.Client.Photon.Hashtable();

            bool needSet = false;
            if (!props.ContainsKey(ROOM_PROP_CREATED_AT))
            {
                props[ROOM_PROP_CREATED_AT] = nowMs; // 최초 생성 시각 룸 프로퍼티에 저장
                needSet = true;
            }
            if (!props.ContainsKey(ROOM_PROP_ANCHOR_UID))
            {
                var anchor = PhotonNetwork.MasterClient?.UserId ?? _fsm?.MyUid;
                props[ROOM_PROP_ANCHOR_UID] = anchor;
                needSet = true;
            }

            if (needSet && PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                Debug.Log($"[MatchClient] RoomProperty 설정: createdAt={props[ROOM_PROP_CREATED_AT]}, anchorUid={props[ROOM_PROP_ANCHOR_UID]}");
            }
        }

        private string CalculateHomeRoomName()
        {
            string myUid = _fsm?.MyUid ?? Guid.NewGuid().ToString("N");
            string partyId = _currentPartyId ?? $"solo:{myUid}";
            return $"p_{partyId}_{myUid.Substring(0, 6)}";
        }

        private bool IsInHomeRoom()
        {
            return PhotonNetwork.InRoom &&
                   PhotonNetwork.CurrentRoom != null &&
                   PhotonNetwork.CurrentRoom.Name == CalculateHomeRoomName();
        }

        private IEnumerator ReturnToHomeRoom()
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom(false);
                yield return new WaitUntil(() => !PhotonNetwork.InRoom);
            }

            if (PhotonNetwork.NetworkClientState != ClientState.ConnectedToMasterServer)
            {
                yield return new WaitUntil(() =>
                PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer);
            }

            string roomName = CalculateHomeRoomName();
            var opt = new RoomOptions
            {
                MaxPlayers = _me?.max > 0 ? _me.max : MAX_PLAYERS,
                IsOpen = true,
                IsVisible = true,
                PublishUserId = true
            };

            PhotonNetwork.JoinOrCreateRoom(roomName, opt, TypedLobby.Default);
            yield return new WaitUntil(() =>
                PhotonNetwork.InRoom &&
                PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.Name == roomName);

            EnsureMeFromPhoton(); // _me 동기화

            if (PhotonNetwork.IsMasterClient)
            {
                if (_party?.Members.Count > 0)
                {
                    PhotonNetwork.CurrentRoom.SetExpectedUsers((_party?.Members).ToArray());
                }
                else
                {
                    PhotonNetwork.CurrentRoom.ClearExpectedUsers();
                }
            }
        }

        private IEnumerator ReturnToSpecificRoom(string roomName)
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom(false);
                yield return new WaitUntil(() => !PhotonNetwork.InRoom);
            }
            var opt = new RoomOptions
            {
                MaxPlayers = _me?.max > 0 ? _me.max : MAX_PLAYERS,
                IsOpen = true,
                IsVisible = true,
                PublishUserId = true
            };
            PhotonNetwork.JoinOrCreateRoom(roomName, opt, TypedLobby.Default);
            yield return new WaitUntil(() => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom?.Name == roomName);
            EnsureMeFromPhoton();
        }

        private void NotifyMatchStopToAll(string reason)
        {
            // 현재 방의 다른 플레이 수집
            var targets = new HashSet<string>();
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom?.Players != null)
            {
                foreach (var kv in PhotonNetwork.CurrentRoom.Players)
                {
                    string uid = kv.Value?.UserId;
                    if (!string.IsNullOrEmpty(uid) && uid != _fsm.MyUid)
                    {
                        targets.Add(uid);
                    }
                }
            }

            // 아직 방에 못 들어온 게스트 수집
            foreach (var uid in _lastExpectedUids)
            {
                if (!string.IsNullOrEmpty(uid) && uid != _fsm.MyUid)
                {
                    targets.Add(uid);
                }
            }

            // 파티 인원들은 제외
            HashSet<string> party = new(_party?.Members);
            targets.RemoveWhere(uid => party.Contains(uid));

            var msg = new MatchMessages.MatchCancelMsg
            {
                matchId = _lastTicketMatchId,
                reason = reason,
                uids = targets.ToList()
            };

            foreach (var uid in targets)
            {
                Chat.SendDM(uid, msg);
            }

            Debug.Log($"[MatchClient] MATCH_CANCEL 발신 reason: {reason}, to: {string.Join(",", targets)}");
        }

        private void AnchorRoomToMyself()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;
            if (!PhotonNetwork.IsMasterClient) return;

            var props = PhotonNetwork.CurrentRoom.CustomProperties
                        ?? new ExitGames.Client.Photon.Hashtable();

            var myUid = _fsm?.MyUid ?? FirebaseManager.Instance.Auth.CurrentUser.UserId;
            props[ROOM_PROP_ANCHOR_UID] = myUid;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            if (_me != null) _me.anchorUid = myUid;
            Debug.Log($"[MatchClient] 앵커 승계 완료, anchorUid: {myUid}");
        }

        #endregion
    }
}
