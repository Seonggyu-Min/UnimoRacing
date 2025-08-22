using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class MatchMessages : MonoBehaviour
    {

        [Serializable]
        public class LfgMsg
        {
            public string t = "LFG";
            public string id;              // uuid
            public string partyId;
            public string room;
            public string leaderUid;
            public string[] uids;
            public int size;
            public int max;
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
            public string[] uids;
            public long exp;
        }
    }
}
