using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MSG.MatchMessages;
using static UnityEngine.Rendering.DebugUI.Table;


namespace MSG
{
    public enum MatchState { Idle, Locking, WaitingAck, Whitelisting, Ticketing, Joining }


    public class MatchFSM
    {
        // 콜백 주입
        public Action<string, object> Publish;           // 채널 송신
        public Action<string, object> SendDM;            // DM 송신
        public Func<long> NowMs;                         // 시간
        public Action<string[]> SetExpectedUsers;        // PUN 화이트리스트 확장
        public Action<string, long> TryJoinRoomUntil;    // 티켓 Join 재시도
        public Func<string, LfgMsg> FindLfgByRoom;       // room -> LFG
        public Func<LfgMsg> GetMyLfg;                    // 내 LFG
        public Action CloseMyRoomForMerge;               // 게스트 ACK 후 내 방 닫기 훅 (추가)
        public Action<string[], int> UpdateLocalPartyAdvertised; // 사이즈, 멤버 갱신용
        private readonly Dictionary<string, long> _lockCooldown = new(); // 같은 쌍 재시도 방지, key: myRoom|otherRoom -> untilMs
        public string ChannelName;
        public string MyUid;

        public MatchState State { get; private set; } = MatchState.Idle;

        private string _pendingMatchId;
        private LfgMsg _pendingOther;

        // ACK 타임아웃 관리
        private long _ackDeadlineMs = 0;
        public void Tick()
        {
            if (State == MatchState.WaitingAck && NowMs() > _ackDeadlineMs)
            {
                // 타임아웃 -> 대기 취소
                State = MatchState.Idle;
                _pendingMatchId = null;
                _pendingOther = null;
            }
        }

        public void TryMatchWith(LfgCache cache)
        {
            if (State != MatchState.Idle) return;

            var me = GetMyLfg?.Invoke();
            if (me == null) return;
            long now = NowMs();

            foreach (var other in cache.Alive(NowMs()))
            {
                if (other.partyId == me.partyId) continue;
                if (other.room == me.room) continue; // 자기 방 LFG 방지
                if (other.leaderUid == me.leaderUid) continue; // 리더 UID 동일 방지

                int sum = me.size + other.size;

                // 부분 합류 허용
                if (sum > me.max)
                {
                    // 넘치면 스킵
                    Debug.Log($"[매치] 합치면 {sum}/{me.max} → 초과라 스킵 (내:{me.size}, 상대:{other.size})");
                    continue;
                }

                if (IAmHost(me, other))
                {
                    if (IsCoolingDown(me.room, other.room, now))
                    {
                        // 연속으로 LOCK 시도하지 않도록 제동
                        continue;
                    }

                    Debug.Log($"[매치] 내가 호스트 조건 → LOCK 시도 (내방:{me.room}, 상대방:{other.room}, 합치면 {sum}/{me.max})");
                    SetCooldown(me.room, other.room, now, 2000); // 2초 쿨다운
                    LockAsHost(me, other);
                    break;
                }
                else
                {
                    Debug.Log($"[매치] 비호스트 → LOCK 대기 (내방:{me.room}, 상대방:{other.room}, 합치면 {sum}/{me.max})");
                    // 비호스트면 상대 LOCK 기다림. 다른 후보도 보고 싶다면 continue 가능
                    continue;
                }
            }
        }

        public void OnLock(MatchLockMsg m)
        {
            var me = GetMyLfg?.Invoke();
            if (me == null) return;
            if (m.guestRoom != me.room) return; // 내 방이 대상이 아니면 무시

            var other = FindLfgByRoom?.Invoke(m.hostRoom);
            if (other == null)
            {
                Debug.LogWarning("[LOCK] 게스트가 LOCK 수신했지만 호스트 정보를 못 찾음");
                return;
            }

            if (IAmHost(me, other))
            {
                // 동시 LOCK -> NACK
                SendDM?.Invoke(other.leaderUid, new MatchAckMsg { matchId = m.matchId, ok = false });
                Debug.Log("[ACK] 내가 호스트라서 충돌 → NACK 보냄");
                return;
            }

            // 게스트: ACK 보내고 방 닫기 콜백 호출
            SendDM?.Invoke(other.leaderUid, new MatchAckMsg { matchId = m.matchId, ok = true });
            CloseMyRoomForMerge?.Invoke();    // 새 입장 차단(IsOpen=false 등)
            State = MatchState.Joining;       // 티켓 대기 단계
            Debug.Log($"[ACK] 게스트가 ACK 보냄 → 매치ID:{m.matchId}, 상태 전환: Joining");
        }

        public void OnAck(MatchAckMsg a)
        {
            if (State != MatchState.WaitingAck || a.matchId != _pendingMatchId)
            {
                Debug.LogWarning($"[ACK] ACK 무시됨 (현재 상태:{State}, 매치ID:{a.matchId})");
                return;
            }

            if (!a.ok)
            {
                Debug.Log("[ACK] NACK 수신 → Idle로 복귀");
                State = MatchState.Idle; _pendingMatchId = null; _pendingOther = null;
                return;
            }

            // 화이트리스트 확장
            State = MatchState.Whitelisting;
            var me = GetMyLfg?.Invoke();
            if (me == null || _pendingOther == null) { State = MatchState.Idle; return; }

            var merged = Merge(me.uids, _pendingOther.uids);
            SetExpectedUsers?.Invoke(merged);
            Debug.Log($"[FSM] 상태 전환: Whitelisting → {string.Join(",", merged)}");

            // 호스트 광고 업데이트: 합치면 몇 명이 되는지 갱신 (정원이 안 찼으면 계속 LFG 발행)
            int newSize = me.size + _pendingOther.size;
            UpdateLocalPartyAdvertised?.Invoke(merged, newSize);

            // 티켓 발급 (게스트는 내 방으로 조인)
            State = MatchState.Ticketing;
            var ticket = new TicketMsg
            {
                matchId = _pendingMatchId,
                room = me.room,
                uids = _pendingOther.uids,
                exp = NowMs() + 10000
            };
            SendDM?.Invoke(_pendingOther.leaderUid, ticket);
            Debug.Log($"[TICKET] 호스트가 티켓 발행 → 방:{me.room}, 매치ID:{_pendingMatchId}");

            State = MatchState.Idle;
            _pendingMatchId = null;
            _pendingOther = null;
            Debug.Log("[FSM] 상태 전환: Idle (대기)");
        }

        public void OnTicket(TicketMsg t)
        {
            if (Array.IndexOf(t.uids, MyUid) < 0) return;
            State = MatchState.Joining;
            Debug.Log($"[TICKET] 게스트가 티켓 받음 → 방:{t.room}, 매치ID:{t.matchId}, Join 시도 시작");
            TryJoinRoomUntil?.Invoke(t.room, t.exp);
        }


        private void LockAsHost(LfgMsg me, LfgMsg other)
        {
            State = MatchState.Locking;
            _pendingMatchId = Guid.NewGuid().ToString("N");
            _pendingOther = other;

            var msg = new MatchLockMsg
            {
                matchId = _pendingMatchId,
                hostRoom = me.room,
                guestRoom = other.room,
                hostKey = $"{me.roomCreatedAt}:{me.leaderUid}",
                ts = NowMs()
            };
            Publish?.Invoke(ChannelName, msg);
            
            Debug.Log($"[LOCK] 내가 호스트로 LOCK 발행 → 호스트방:{me.room}, 게스트방:{other.room}, 매치ID:{_pendingMatchId}");

            // WAITING으로 전환, 타임아웃 설정
            State = MatchState.WaitingAck;
            _ackDeadlineMs = NowMs() + 3000; // 3초 대기
            Debug.Log($"[FSM] 상태 전환: WaitingAck (3초 동안 ACK 대기)");
        }

        private bool IAmHost(LfgMsg me, LfgMsg other)
        {
            int c = me.roomCreatedAt.CompareTo(other.roomCreatedAt);
            if (c != 0) return c < 0;
            return string.CompareOrdinal(me.leaderUid, other.leaderUid) < 0;
        }

        private string[] Merge(string[] a, string[] b)
        {
            var arr = new string[(a?.Length ?? 0) + (b?.Length ?? 0)];
            int i = 0;
            if (a != null) { Array.Copy(a, 0, arr, i, a.Length); i += a.Length; }
            if (b != null) { Array.Copy(b, 0, arr, i, b.Length); }
            return arr;
        }

        private bool IsCoolingDown(string myRoom, string otherRoom, long nowMs)
        {
            var key = myRoom + "|" + otherRoom;
            if (_lockCooldown.TryGetValue(key, out var until) && until > nowMs) return true;
            return false;
        }

        private void SetCooldown(string myRoom, string otherRoom, long nowMs, int ms = 2000)
        {
            var key = myRoom + "|" + otherRoom;
            _lockCooldown[key] = nowMs + ms;
        }
    }
}
