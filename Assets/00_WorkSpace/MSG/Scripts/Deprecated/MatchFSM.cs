using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MSG.Deprecated
{
    public enum MatchState
    {
        None,           // 매칭 아닌 상태
        Finding,        // 매칭 대기 상태 
        Locking,        // 합류 제안을 보낸 상태
        WaitingAck,     // 합류 제안을 보낸 뒤 ACK 답장 기다리는 상태
        Whitelisting,   // ACK를 받고 화이트리스트 작업 중인 상태
        Ticketing,      // 합류 제안에 대해 ACK을 받고 자신의 방 정보를 전송하는 상태
        Joining         // ACK를 보내고 티켓을 받고 해당 방에 참가 중인 상태
    }


    public class MatchFSM
    {
        public MatchState State { get; private set; } = MatchState.None;

        // MatchClient의 콜백
        public Action<string, object> Publish;                              // 채널 송신
        public Action<string, object> SendDM;                               // DM 송신
        public Func<long> NowMs;                                            // 현재 시간
        public Action<List<string>> SetExpectedUsers;                       // PUN 화이트리스트 확장
        public Action<string, long, Action<bool>> TryJoinRoomUntil;         // 티켓 Join 재시도
        public Func<MatchMessages.LfgMsg> GetMyLfg;                         // 내 LFG 조회용
        public Action CloseMyRoomForMerge;                                  // 게스트 ACK 후 내 방 닫기용
        public Action<List<string>, int> UpdateLocalPartyAdvertised;        // 사이즈, 멤버 갱신용
        public Action<string, List<string>, long> BeginObserve;             // Join 관찰용                                       
        public Action OnGuestTicketTimeout;                                 // 게스트 티켓 대기 타임아웃 알림용

        private readonly Dictionary<string, long> _lockCooldown = new();    // 같은 쌍 재시도 방지, key: myRoom|otherRoom -> untilMs

        public string ChannelName;
        public string MyUid;
        private string _pendingMatchId;
        private string _pendingMyRoom;                      // 내 방 저장용
        private MatchMessages.LfgMsg _pendingOther;
        private long _ackDeadlineMs = 0;                    // ACK 타임아웃 관리
        private long _guestTicketDeadlineMs = 0;            // 게스트가 ACK 이후 티켓 대기 마감 시간

        private const int LOCK_COOLDOWN_MS = 2000;          // 특정 페어에 대한 LOCK 재시도 쿨다운
        private const int ACK_TIMEOUT_MS = 3000;            // ACK 대기 시간
        private const int TICKET_TTL_MS = 10000;            // 티켓 만료 시간
        private const int FAIL_GRACE_MS = 2000;             // 티켓 만료 시간에 더하는 접속 실패 용인 시간
        private const int GUEST_TICKET_WAIT_MS = 10000;      // 티켓 대기 시간


        public void Tick()
        {
            // 상대 ACK 수신 대기 타임아웃
            if (State == MatchState.WaitingAck && NowMs() > _ackDeadlineMs)
            {
                MatchMessages.LfgMsg meNow = GetMyLfg?.Invoke();
                if (meNow != null && _pendingOther != null)
                {
                    SetCooldownPair(meNow.room, _pendingOther.room, NowMs(), LOCK_COOLDOWN_MS);
                }

                State = MatchState.Finding;
                _pendingMatchId = null;
                _pendingMyRoom = null;
                _pendingOther = null;

                Debug.Log("[FSM] WaitingAck 타임아웃되어 Finding 복귀");
            }

            if (State == MatchState.Joining &&    // 합류 중인데
                _guestTicketDeadlineMs > 0 &&     // 타이머가 설정 되었고
                NowMs() > _guestTicketDeadlineMs) // 타이머가 다 됐으면
            {
                _guestTicketDeadlineMs = 0;     // 타이머 해제
                State = MatchState.Finding;     // 상태 복구
                OnGuestTicketTimeout?.Invoke(); // 타임아웃

                Debug.Log("[FSM] (게스트) 티켓 미수신 타임아웃 -> Finding 복귀 + 방 재오픈 요청");
            }
        }

        public void EnableMatching()
        {
            if (State == MatchState.None)
            {
                State = MatchState.Finding; // 매칭 시작
            }
            else
            {
                Debug.LogWarning($"[FSM] 현재 상태 {State}일 때 매치 시작 호출됨. 중복 호출 여부 확인 요망");
            }
        }

        public void DisableMatching()
        {
            State = MatchState.None;
            _pendingMatchId = null;
            _pendingMyRoom = null;
            _pendingOther = null;
            _ackDeadlineMs = 0;
            _guestTicketDeadlineMs = 0;
            Debug.Log("[FSM] None으로 복귀");
        }

        // 다른 파티 혹은 솔로와 매칭 시도
        public void TryMatchWith(LfgCache cache)
        {
            if (State != MatchState.Finding) return; // 내가 대기 중이지 않으면 return

            var me = GetMyLfg?.Invoke(); // 내 LFG 조회
            if (me == null) return;
            long now = NowMs();

            foreach (var other in cache.Alive(now)) // 만료되지 않은 LFG 후보 순회
            {
                // 조건 검사
                if (other.partyId == me.partyId) continue;      // 내 파티가 발행한 LFG는 continue
                if (other.room == me.room) continue;            // 내 방은 continue

                int sum = me.size + other.size;
                bool amHost = IAmHost(me.anchorUid, other.anchorUid);
                int hostMax = amHost ? me.max : other.max;

                // 다른 방과 합쳤을 때
                if (sum > hostMax)
                {
                    // 넘치면 스킵
                    Debug.Log($"[매치] 합치면 {sum}/{me.max} -> 초과라 스킵 (내:{me.size}, 상대:{other.size})");
                    continue;
                }

                if (IsOnCooldownPair(me.room, other.room, now)) continue; // 쿨다운 동안 매칭 시도 중지

                // 내가 호스트라면
                if (IAmHost(me.anchorUid, other.anchorUid))
                {
                    Debug.Log($"[매치] 내가 호스트 조건 -> LOCK 시도 (내방:{me.room}, 상대방:{other.room}, 합치면 {sum}/{me.max})");

                    SetCooldownPair(me.room, other.room, now, LOCK_COOLDOWN_MS); // 중복 LOCK 중지용
                    LockAsHost(me, other); // WaitingAck로 상태 전환
                    break;
                }
                else
                {
                    Debug.Log($"[매치] 비호스트 -> LOCK 대기 (내방:{me.room}, 상대방:{other.room}, 합치면 {sum}/{me.max})");
                    continue;
                }
            }
        }

        // LOCK 수신
        public void OnLock(MatchMessages.MatchLockMsg m)
        {
            if (State == MatchState.None)
            {
                Debug.Log("[LOCK] 매칭이 취소된 상태라 무시");
                return;
            }

            var me = GetMyLfg?.Invoke();
            if (me == null) return;

            if (!string.Equals(m.guestRoom, me.room))
                return;

            // Finding이 아닐 때는 즉시 NACK
            if (State != MatchState.Finding)
            {
                if (!string.IsNullOrEmpty(m.hostContactUid))
                {
                    SendDM?.Invoke(m.hostContactUid, new MatchMessages.MatchAckMsg { matchId = m.matchId, ok = false });
                }

                SetCooldownPair(me.room, m.hostRoom, NowMs(), LOCK_COOLDOWN_MS);
                Debug.Log($"[LOCK] 상태: {State} -> NACK 전송 및 무시");
                return;
            }

            // 사전순으로 더 작은 uid가 호스트
            // 즉, 내가 더 작으면 내가 호스트여야 하므로 상대 LOCK을 NACK
            var myUid = MyUid;
            if (string.CompareOrdinal(myUid, m.hostContactUid) < 0)
            {
                SendDM?.Invoke(m.hostContactUid, new MatchMessages.MatchAckMsg { matchId = m.matchId, ok = false });
                SetCooldownPair(me.room, m.hostRoom, NowMs(), LOCK_COOLDOWN_MS);
                Debug.Log("[LOCK] 타이브레이커에 따라 내가 호스트 -> 상대 LOCK NACK");
                return;
            }

            // 게스트로서 수락
            if (!string.IsNullOrEmpty(m.hostContactUid))
            {
                SendDM?.Invoke(m.hostContactUid, new MatchMessages.MatchAckMsg { matchId = m.matchId, ok = true });
            }

            CloseMyRoomForMerge?.Invoke();       // 새 입장 차단
            State = MatchState.Joining;          // 티켓 대기
            _guestTicketDeadlineMs = NowMs() + GUEST_TICKET_WAIT_MS;
            Debug.Log($"[ACK] 게스트 ACK -> matchId: {m.matchId}, 상태: Joining, 티켓기한: {_guestTicketDeadlineMs}");
        }

        // ACK 수신
        public void OnAck(MatchMessages.MatchAckMsg a)
        {
            if (State == MatchState.None)
            {
                Debug.Log("[ACK] ACK 수신하였으나 매칭이 취소되어 return");
                return;
            }

            // Ack을 기다리고 있지 않거나 오래되었다면 무시
            if (State != MatchState.WaitingAck || a.matchId != _pendingMatchId)
            {
                Debug.LogWarning($"[ACK] ACK 무시됨 (현재 상태:{State}, 매치ID:{a.matchId})");
                return;
            }

            // NACK 수신 시 Idle 복귀 및 쿨다운 적용
            if (!a.ok)
            {
                if (_pendingMyRoom != null && _pendingOther != null)
                {
                    SetCooldownPair(_pendingMyRoom, _pendingOther.room, NowMs(), LOCK_COOLDOWN_MS);
                }

                State = MatchState.Finding;
                _pendingMatchId = null;
                _pendingOther = null;
                _pendingMyRoom = null;

                Debug.Log("[ACK] NACK 수신 -> Finding으로 복귀");
                return;
            }

            // ACK 수신 시 화이트리스트 확장
            State = MatchState.Whitelisting;

            var me = GetMyLfg?.Invoke();
            if (me == null || _pendingOther == null)
            {
                State = MatchState.Finding;
                _pendingMatchId = null;
                _pendingMyRoom = null;
                _pendingOther = null;
                Debug.LogWarning("[FSM] Whitelisting 전 me 혹은 other 누락 -> Finding 복귀");
                return;
            }

            // 양쪽 Uid 합치기
            var merged = Merge(me.uids, _pendingOther.uids);

            SetExpectedUsers?.Invoke(merged);
            Debug.Log($"[FSM] 상태 전환: Whitelisting -> {string.Join(",", merged)}");

            // 들어오려는 사람 모두에게 티켓 발급하여 각자 참여 시도
            State = MatchState.Ticketing;
            var ticket = new MatchMessages.TicketMsg
            {
                matchId = _pendingMatchId,
                room = me.room,
                uids = _pendingOther.uids,
                exp = NowMs() + TICKET_TTL_MS
            };

            // 게스트 전원에게 DM
            if (_pendingOther.uids != null)
            {
                foreach (string uid in _pendingOther.uids)
                {
                    SendDM?.Invoke(uid, ticket);
                }
            }

            Debug.Log($"[TICKET] 호스트가 티켓 발행 -> 방:{me.room}, 매치ID:{_pendingMatchId}");

            long failTime = NowMs() + TICKET_TTL_MS + FAIL_GRACE_MS;
            BeginObserve?.Invoke(_pendingMatchId, merged, failTime);

            // 이후 Idle 복귀
            State = MatchState.Finding;
            _pendingMatchId = null;
            _pendingMyRoom = null;
            _pendingOther = null;
            Debug.Log("[FSM] 상태 전환: Finding (대기)");
        }

        // 티켓 수신
        public void OnTicket(MatchMessages.TicketMsg t)
        {
            if (State == MatchState.None)
            {
                Debug.Log("[TICKET] TICKET 수신하였으나 매칭이 취소되어 return");
                return;
            }

            if (t.uids == null || !t.uids.Contains(MyUid)) return; // 내 UID가 포함되어있지 않으면 return

            Debug.Log($"[TICKET] 게스트가 티켓 받음 -> 방:{t.room}, 매치ID:{t.matchId}, Join 시도 시작");
            State = MatchState.Joining;
            _guestTicketDeadlineMs = 0; // 티켓 타이머 리셋

            TryJoinRoomUntil?.Invoke(t.room, t.exp, ok =>
            {
                if (!ok)
                {
                    State = MatchState.Finding;
                    Debug.Log("[FSM] Join 만료 혹은 실패 -> Finding 복귀");
                }
            });
        }

        public void OnMatchCancelFromHost(string matchId)
        {
            if (State == MatchState.Joining)
            {
                State = MatchState.Finding;
                Debug.Log("[FSM] MATCH_CANCEL 수신, 상태 전환: Finding (대기)");
            }
        }


        private void LockAsHost(MatchMessages.LfgMsg me, MatchMessages.LfgMsg other)
        {
            State = MatchState.Locking;
            _pendingMatchId = Guid.NewGuid().ToString("N");
            _pendingOther = other;
            _pendingMyRoom = me.room;

            var msg = new MatchMessages.MatchLockMsg
            {
                matchId = _pendingMatchId,
                hostRoom = me.room,
                guestRoom = other.room,
                hostContactUid = MyUid,
                ts = NowMs()
            };

            Publish?.Invoke(ChannelName, msg); // 채널에 Lock 발신

            Debug.Log($"[LOCK] 내가 호스트로 LOCK 발행 -> 호스트방:{me.room}, 게스트방:{other.room}, 매치ID:{_pendingMatchId}");

            // WaitingAck로 상태 전환, 타임아웃 설정
            State = MatchState.WaitingAck;
            _ackDeadlineMs = NowMs() + ACK_TIMEOUT_MS; // 잠깐 ACK를 받을 수 있도록 대기
            Debug.Log($"[FSM] 상태 전환: WaitingAck (잠깐 ACK 대기)");
        }

        // 방 생성 시점이 오래된 사람이 호스트가 되도록 결정
        private bool IAmHost(string me, string other)
        {
            return string.CompareOrdinal(me, other) < 0;
        }

        // 중복 제거
        private List<string> Merge(List<string> a, List<string> b)
        {
            var set = new HashSet<string>();
            if (a != null) set.UnionWith(a);
            if (b != null) set.UnionWith(b);
            return set.ToList();
        }

        // 쿨다운 키의 순서 보장
        private string PairKey(string roomA, string roomB)
        {
            return string.CompareOrdinal(roomA, roomB) < 0
                ? $"{roomA}|{roomB}"
                : $"{roomB}|{roomA}";
        }

        // 쿨다운 적용
        private void SetCooldownPair(string roomA, string roomB, long nowMs, int ms)
        {
            string key = PairKey(roomA, roomB);
            _lockCooldown[key] = nowMs + ms;
        }

        // 쿨다운 중인지 판단
        private bool IsOnCooldownPair(string roomA, string roomB, long nowMs)
        {
            string key = PairKey(roomA, roomB);
            return _lockCooldown.TryGetValue(key, out long until) && until > nowMs;
        }
    }
}
