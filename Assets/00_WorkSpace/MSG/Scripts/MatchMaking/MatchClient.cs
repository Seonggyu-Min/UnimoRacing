using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MSG.MatchMessages;


namespace MSG
{
    public class MatchClient : MonoBehaviour
    {
        [Header("Chat")]
        [SerializeField] private ServerSettings serverSettings;
        [SerializeField] private string _region = "asia";
        [SerializeField] private string _channel = "mm-standard";
        [SerializeField] private int _history = 10;

        [Header("LFG")]
        [SerializeField] private int _lfgTtlMs = 15000;
        [SerializeField] private float _lfgIntervalSec = 3f;

        public ChatService Chat { get; private set; }
        private LfgCache _cache;
        private MatchFSM _fsm;
        private LfgMsg _me;
        private bool _leavingForMerge = false;

        // 초기 설정
        public void SetLocalPartyRoom(string partyId, string room, string leaderUid, string[] uids, int size, int max, long roomCreatedAt)
        {
            var now = NowMs();
            _me = new LfgMsg
            {
                id = System.Guid.NewGuid().ToString("N"),
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

        void Start()
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

        void Update()
        {
            Chat?.Service();
            _fsm?.Tick(); // WAITING 타임아웃 체크
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

            // 첫 LFG 즉시
            Debug.Log($"[매치] LFG 발행 시작 (주기: {_lfgIntervalSec}초)");
            BroadcastLfg();
            // 주기 브로드캐스트 시작
            InvokeRepeating(nameof(BroadcastLfg), _lfgIntervalSec, _lfgIntervalSec);
        }

        private void BroadcastLfg()
        {
            if (_me == null || string.IsNullOrEmpty(_me.partyId)) return;
            var now = NowMs();
            _me.ts = now;
            _me.expiresAt = now + _lfgTtlMs;
            Chat.Publish(_channel, _me);
            Debug.Log($"[LFG] 내가 방({_me.room}) 정보를 발행함. 파티ID:{_me.partyId}, 인원:{_me.size}/{_me.max}");
        }

        private void OnMatchChannel(string sender, object raw)
        {
            var json = raw as string;
            if (string.IsNullOrEmpty(json)) return;

            if (json.Contains("\"t\":\"LFG\""))
            {
                var msg = JsonUtility.FromJson<LfgMsg>(json);
                _cache.Upsert(msg, NowMs());
                Debug.Log($"[LFG] 다른 파티 LFG 수신: 파티ID:{msg.partyId}, 방:{msg.room}, 인원:{msg.size}/{msg.max}");
                _fsm.TryMatchWith(_cache);
            }
            else if (json.Contains("\"t\":\"MATCH_LOCK\""))
            {
                var m = JsonUtility.FromJson<MatchLockMsg>(json);
                Debug.Log($"[LOCK] LOCK 메시지 수신 → 호스트방:{m.hostRoom}, 게스트방:{m.guestRoom}, 매치ID:{m.matchId}");
                _fsm.OnLock(m);
            }
            else if (json.Contains("\"t\":\"MATCH_ACK\""))
            {
                var a = JsonUtility.FromJson<MatchAckMsg>(json);
                Debug.Log($"[ACK] ACK 메시지 수신 → 매치ID:{a.matchId}, 결과:{a.ok}");
                _fsm.OnAck(a);
            }
        }

        private void OnDM(string sender, object raw)
        {
            var json = raw as string;
            if (string.IsNullOrEmpty(json)) return;

            if (json.Contains("\"t\":\"TICKET\""))
            {
                var t = JsonUtility.FromJson<TicketMsg>(json);
                Debug.Log($"[TICKET] 티켓 수신 → 방:{t.room}, 만료:{t.exp}, 매치ID:{t.matchId}");
                _fsm.OnTicket(t);
            }
            else if (json.Contains("\"t\":\"MATCH_ACK\""))
            {
                var a = JsonUtility.FromJson<MatchAckMsg>(json);
                _fsm.OnAck(a);
            }
        }

        // PUN 연동
        private void SetExpectedUsersViaPun(string[] mergedUids)
        {
            Debug.Log($"[FSM] SetExpectedUsers: {string.Join(",", mergedUids)}");
            PhotonNetwork.CurrentRoom.SetExpectedUsers(mergedUids);
        }

        private void TryJoinRoomUntilViaPun(string room, long expMs)
        {
            StartCoroutine(JoinRetry(room, expMs));
        }

        private IEnumerator JoinRetry(string room, long expMs)
        {
            int attempt = 0;

            // TODO: 아래 WaitUntil말고 콜백 받아야될 듯, 근데 일단 바로 테스트용으로 씀
            // 아직 방 안에 있으면, 완전히 나갈 때까지 대기
            Debug.Log("[PUN] 자신 Room에서 나올 때 까지 대기");

            if (PhotonNetwork.InRoom)
            {
                yield return new WaitUntil(() => !PhotonNetwork.InRoom);
            }

            Debug.Log("[PUN] Master에 연결될 때 까지 대기");

            // Master에 연결될 때까지 대기
            if (PhotonNetwork.NetworkClientState != Photon.Realtime.ClientState.ConnectedToMasterServer &&
                   PhotonNetwork.NetworkClientState != Photon.Realtime.ClientState.JoinedLobby)
            {
                yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.ConnectedToMasterServer &&
                PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.JoinedLobby
                );
            }

            Debug.Log($"[PUN] 현재 클라이언트 상태: {PhotonNetwork.NetworkClientState.ToString()}");

            _leavingForMerge = false; // 방에서 완전히 나왔음

            // 유효시간 동안 Join 재시도
            while (NowMs() < expMs)
            {
                attempt++;
                Debug.Log($"[PUN] 방 합류 시도 {attempt}회 → {room}");
                PhotonNetwork.JoinRoom(room);

                // 성공/실패 콜백을 기다리며 짧게 대기 (실패 시 루프 계속)
                // backoff를 조정도 가능할 듯
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
            try
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;

                // 합류 중 상태를 커스텀 프로퍼티로 표시하면, 로컬/원격 로직에서 참고하기 쉬움
                var props = new ExitGames.Client.Photon.Hashtable
                {
                    { "merging", true }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);

                Debug.Log("[PUN] 내 방 닫기 완료: IsOpen=false, IsVisible=false, merging=true");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PUN] 내 방 닫기 중 예외: {e}");
            }

            // 다른 방으로 이동하기 위해 우선 현재 방에서 나가기
            if (PhotonNetwork.InRoom)
            {
                _leavingForMerge = true;
                PhotonNetwork.LeaveRoom(false);
                Debug.Log("[PUN] 내 방 나가기 요청(merge 준비)");
            }
        }

        private void OnUpdateLocalPartyAdvertised(string[] mergedUids, int newSize)
        {
            if (_me == null) return;

            _me.uids = mergedUids;
            _me.size = newSize;

            Debug.Log($"[LFG] 호스트 광고 갱신: 인원 {newSize}/{_me.max}, 멤버: {string.Join(",", mergedUids)}");

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

        private LfgMsg FindLfgByRoom(string room)
        {
            foreach (var m in _cache.Alive(NowMs()))
                if (m.room == room) return m;
            return null;
        }

        private long NowMs() => System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
