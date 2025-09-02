using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG.Deprecated
{
    public class LfgCache
    {
        private readonly int _ttlMs;
        private readonly Dictionary<string, MatchMessages.LfgMsg> _byRoom = new(); // room으로 키

        public LfgCache(int ttlMs = 15000) { _ttlMs = ttlMs; }

        public void Upsert(MatchMessages.LfgMsg msg, long nowMs)
        {
            msg.expiresAt = nowMs + _ttlMs;
            _byRoom[msg.room] = msg; // 같은 room의 새로운 LFG는 덮어쓰기
        }

        public IEnumerable<MatchMessages.LfgMsg> Alive(long nowMs)
        {
            foreach (var kv in _byRoom)
            {
                if (kv.Value.expiresAt >= nowMs)
                {
                    Debug.Log($"[LFG CACHE] {kv.Value.id}는 살아있어서 평가 대상");
                    yield return kv.Value;
                }
                else
                {
                    Debug.Log($"[LFG CACHE] {kv.Value.id}는 만료되어서 평가 대상 제외");
                }
            }
        }

        public void Sweep(long nowMs)
        {
            var toRemove = new List<string>();
            foreach (var kv in _byRoom)
                if (kv.Value.expiresAt < nowMs) toRemove.Add(kv.Key);
            foreach (var k in toRemove) _byRoom.Remove(k);
        }

        public MatchMessages.LfgMsg FindByRoom(string room) => _byRoom.TryGetValue(room, out var v) ? v : null;
    }
}
