using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MSG
{
    public enum MatchState
    {
        None,           // 매칭 아닌 상태
        Idle,           // 매칭 대기 상태 
        Locking,        // 합류 제안을 보낸 상태
        WaitingAck,     // 합류 제안을 보낸 뒤 ACK 답장 기다리는 상태
        Whitelisting,   // ACK를 받고 화이트리스트 작업 중인 상태
        Ticketing,      // 합류 제안에 대해 ACK을 받고 자신의 방 정보를 전송하는 상태
        Joining         // ACK를 보내고 티켓을 받고 해당 방에 참가 중인 상태
    }


    public class MatchFSM
    {
        public MatchState State { get; private set; } = MatchState.Idle;

        // MatchClient의 콜백
        public Action<string, object> Publish;           // 채널 송신
        public Action<string, object> SendDM;            // DM 송신
        public Func<long> NowMs;                         // 현재 시간
        public Action<List<string>> SetExpectedUsers;    // PUN 화이트리스트 확장
        public Action<string, long> TryJoinRoomUntil;    // 티켓 Join 재시도
        public Func<string, MatchMessages.LfgMsg> FindLfgByRoom;       // 캐시에 저장된 특정 방의 LFG 조회
        public Func<MatchMessages.LfgMsg> GetMyLfg;                    // 내 LFG 조회
        public Action CloseMyRoomForMerge;               // 게스트 ACK 후 내 방 닫기
        public Action<List<string>, int> UpdateLocalPartyAdvertised; // 사이즈, 멤버 갱신용


        private readonly Dictionary<string, long> _lockCooldown = new(); // 같은 쌍 재시도 방지, key: myRoom|otherRoom -> untilMs

        public string ChannelName;
        public string MyUid;
        private string _pendingMatchId;
        private string _pendingMyRoom; // 내 방 저장용
        private MatchMessages.LfgMsg _pendingOther;
        private long _ackDeadlineMs = 0; // ACK 타임아웃 관리

        private const int LOCK_COOLDOWN_MS = 2000; // 특정 페어에 대한 LOCK 재시도 쿨다운
        private const int ACK_TIMEOUT_MS = 3000; // ACK 대기 시간
        private const int TICKET_TTL_MS = 10000; // 티켓 만료 시간

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

                State = MatchState.Idle;
                _pendingMatchId = null;
                _pendingMyRoom = null;
                _pendingOther = null;

                //State = MatchState.Idle;
                //_pendingMatchId = null;
                //_pendingOther = null;
                Debug.Log("[FSM] WaitingAck 타임아웃되어 idle 복귀");
            }
        }

        // 매칭 시도
        public void TryMatchWith(LfgCache cache)
        {
            if (State != MatchState.Idle) return; // 내가 대기 중이지 않으면 return

            var me = GetMyLfg?.Invoke(); // 내 LFG 조회
            if (me == null) return;
            long now = NowMs();

            foreach (var other in cache.Alive(now)) // 만료되지 않은 LFG 후보 순회
            {
                // 조건 검사
                if (other.partyId == me.partyId) continue;      // 내 파티가 발행한 LFG는 continue
                if (other.room == me.room) continue;            // 내 방은 continue
                if (other.leaderUid == me.leaderUid) continue;  // 리더 UID가 같으면 continue

                int sum = me.size + other.size;

                // 다른 방과 합쳤을 때
                if (sum > me.max)
                {
                    // 넘치면 스킵
                    Debug.Log($"[매치] 합치면 {sum}/{me.max} -> 초과라 스킵 (내:{me.size}, 상대:{other.size})");
                    continue;
                }

                if (IsOnCooldownPair(me.room, other.room, now)) continue; // 쿨다운 동안 매칭 시도 중지

                // 내가 호스트라면
                if (IAmHost(me, other))
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
            var me = GetMyLfg?.Invoke();
            if (me == null) return;
            if (m.guestRoom != me.room) return; // 내 방이 대상이 아니면 무시

            // Idle이 아니면 NACK 발신
            if (State != MatchState.Idle)
            {
                var otherBusy = FindLfgByRoom?.Invoke(m.hostRoom);
                if (otherBusy != null)
                {
                    SendDM?.Invoke(otherBusy.leaderUid, new MatchMessages.MatchAckMsg { matchId = m.matchId, ok = false });
                    SetCooldownPair(me.room, otherBusy.room, NowMs(), LOCK_COOLDOWN_MS); // 딜레이 설정
                }
                Debug.Log($"[LOCK] Idle 상태가 아니기에 ({State}) -> NACK 전송 및 무시");
                return;
            }

            var other = FindLfgByRoom?.Invoke(m.hostRoom);
            if (other == null)
            {
                Debug.LogWarning("[LOCK] 게스트가 LOCK 수신했지만 호스트 정보를 못 찾음");
                return;
            }

            // 내가 호스트라고 판단되면 NACK 발신
            if (IAmHost(me, other))
            {
                // 동시 LOCK -> NACK
                SendDM?.Invoke(other.leaderUid, new MatchMessages.MatchAckMsg { matchId = m.matchId, ok = false });
                Debug.Log("[ACK] 내가 호스트라서 충돌 -> NACK 보냄");
                return;
            }

            // ACK 보내고 방 닫기 콜백 호출
            SendDM?.Invoke(other.leaderUid, new MatchMessages.MatchAckMsg { matchId = m.matchId, ok = true });
            CloseMyRoomForMerge?.Invoke();    // 새 입장 차단(IsOpen=false 등)
            State = MatchState.Joining;       // 티켓 대기 단계
            Debug.Log($"[ACK] 게스트가 ACK 보냄 -> 매치ID:{m.matchId}, 상태 전환: Joining");
        }

        // ACK 수신
        public void OnAck(MatchMessages.MatchAckMsg a)
        {
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

                State = MatchState.Idle;
                _pendingMatchId = null;
                _pendingOther = null;
                _pendingMyRoom = null;

                Debug.Log("[ACK] NACK 수신 -> Idle로 복귀");
                return;
            }

            // ACK 수신 시 화이트리스트 확장
            State = MatchState.Whitelisting;

            var me = GetMyLfg?.Invoke();
            if (me == null || _pendingOther == null)
            {
                State = MatchState.Idle;
                _pendingMatchId = null;
                _pendingMyRoom = null;
                _pendingOther = null;
                Debug.LogWarning("[FSM] Whitelisting 전 me 혹은 other 누락 -> Idle 복귀");
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

            // 이후 Idle 복귀
            State = MatchState.Idle;
            _pendingMatchId = null;
            _pendingMyRoom = null;
            _pendingOther = null;
            Debug.Log("[FSM] 상태 전환: Idle (대기)");
        }

        // 티켓 수신
        public void OnTicket(MatchMessages.TicketMsg t)
        {
            if (t.uids == null || !t.uids.Contains(MyUid)) return; // 내 UID가 포함되어있지 않으면 return

            Debug.Log($"[TICKET] 게스트가 티켓 받음 -> 방:{t.room}, 매치ID:{t.matchId}, Join 시도 시작");
            State = MatchState.Joining;
            TryJoinRoomUntil?.Invoke(t.room, t.exp);
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
                hostKey = $"{me.roomCreatedAt}:{me.leaderUid}",
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
        private bool IAmHost(MatchMessages.LfgMsg me, MatchMessages.LfgMsg other)
        {
            int c = me.roomCreatedAt.CompareTo(other.roomCreatedAt);
            if (c != 0) return c < 0;
            return string.CompareOrdinal(me.leaderUid, other.leaderUid) < 0; // 만약 같으면 leaderUid 사전 순으로 비교
        }

        // 중복 제거
        private List<string> Merge(List<string> a, List<string> b)
        {
            IEnumerable<string> seqA = a ?? Enumerable.Empty<string>();
            IEnumerable<string> seqB = b ?? Enumerable.Empty<string>();
            return seqA.Concat(seqB).Distinct(StringComparer.Ordinal).ToList();
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
