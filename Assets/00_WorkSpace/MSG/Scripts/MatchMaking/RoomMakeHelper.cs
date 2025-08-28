using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public static class RoomMakeHelper
    {
        public const int MAX_PLAYERS = 2;

        public static string Personal(string uid)
        {
            return $"h_{uid}";
        }

        public static string PartyHome(string leaderUid)
        {
            return $"p_{leaderUid}";
        }

        public static string MatchCandidate()
        {
            return $"m_{DateTime.UtcNow:yyyyMMddHHmm}_{UnityEngine.Random.Range(1000, 9999)}";
        }
    }
}
