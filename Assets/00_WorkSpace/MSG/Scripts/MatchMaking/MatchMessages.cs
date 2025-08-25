using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class MatchMessages
    {

        [Serializable]
        public class LfgMsg
        {
            public string t = "LFG";       // 메시지 타입
            public string id;              // uuid
            public string partyId;         // 파티 이름
            public string room;            // 방 이름
            public string leaderUid;       // 파티장 uid
            public List<string> uids;      // 파티원 uid 목록
            public int size;               // 현재 파티원 수
            public int max;                // 최대 파티원 수
            public long ts;                // unix ms
            public long expiresAt;         // ts + ttl
            public long roomCreatedAt;     // 결정함수용
            public int v = 1;
        }

        [Serializable]
        public class MatchLockMsg
        {
            public string t = "MATCH_LOCK";
            public string matchId;
            public string hostRoom;
            public string guestRoom;
            public string hostKey;         // $"{roomCreatedAt}:{leaderUid}"
            public long ts;
        }

        [Serializable]
        public class MatchAckMsg
        {
            public string t = "MATCH_ACK";
            public string matchId;
            public bool ok;
        }

        [Serializable]
        public class TicketMsg
        {
            public string t = "TICKET";
            public string matchId;
            public string room;
            public List<string> uids;
            public long exp;
        }
    }
}
