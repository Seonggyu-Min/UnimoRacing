using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MSG.MatchMessages;


namespace MSG
{
    public class LfgCache
    {
        private readonly int _ttlMs;
        private readonly Dictionary<string, LfgMsg> _byRoom = new(); // room으로 키

        public LfgCache(int ttlMs = 15000) { _ttlMs = ttlMs; }

        public void Upsert(LfgMsg msg, long nowMs)
        {
            msg.expiresAt = nowMs + _ttlMs;
            _byRoom[msg.room] = msg; // 같은 room의 새로운 LFG는 덮어쓰기
        }

        public IEnumerable<LfgMsg> Alive(long nowMs)
        {
            foreach (var kv in _byRoom)
            {
                if (kv.Value.expiresAt >= nowMs) yield return kv.Value;
            }
        }

        public void Sweep(long nowMs)
        {
            var toRemove = new List<string>();
            foreach (var kv in _byRoom)
                if (kv.Value.expiresAt < nowMs) toRemove.Add(kv.Key);
            foreach (var k in toRemove) _byRoom.Remove(k);
        }

        public LfgMsg FindByRoom(string room) => _byRoom.TryGetValue(room, out var v) ? v : null;
    }
}
