using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG.Deprecated
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
            public string contactUid;      // DM을 받을 대표 uid
            public string anchorUid;       // 방의 원 생성자, 불변 타이브레이커용
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
            public string hostContactUid;
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

        [Serializable]
        public class MatchCancelMsg
        {
            public string t = "MATCH_CANCEL";
            public string matchId;
            public string reason;
            public List<string> uids;
        }

        [Serializable]
        public class PartyRecallMsg
        {
            public string t = "PARTY_RECALL";
            public string room;      // 리더의 방 이름
            public string partyId;
            public string leaderUid;
            public long exp;         // 유효 시간
        }

        [Serializable]
        public class PartyCancelMsg
        {
            public string t = "PARTY_CANCEL";
            public string partyId;
            public string senderUid;
            public string reason;
            public long ts;
        }
    }
}
