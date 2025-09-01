using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public enum RoomType
    {
        Home, 
        Party, 
        Match
    }

    public static class RoomMakeHelper
    {
        public const string ROOM_TYPE = "RoomType";
        public const int MAX_PLAYERS = 4;

        public static string Personal(string uid) => $"h_{uid}";
        public static string PartyHome(string leaderUid) => $"p_{leaderUid}";
        public static string MatchCandidate() => $"m_{DateTime.UtcNow:MMddHHmm}_{UnityEngine.Random.Range(1000, 9999)}";


        public static RoomOptions MakeHomeOptions()
        {
            return new RoomOptions
            {
                IsOpen = true,
                IsVisible = true,
                MaxPlayers = MAX_PLAYERS,
                PublishUserId = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { ROOM_TYPE, RoomType.Home } },
                CustomRoomPropertiesForLobby = new[] { ROOM_TYPE }
            };
        }

        public static RoomOptions MakePartyHomeOptions()
        {
            return new RoomOptions
            {
                IsOpen = true,
                IsVisible = true,
                MaxPlayers = MAX_PLAYERS,
                PublishUserId = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { ROOM_TYPE, RoomType.Party } },
                CustomRoomPropertiesForLobby = new[] { ROOM_TYPE }
            };
        }

        public static RoomOptions MakeMatchOptions()
        {
            return new RoomOptions
            {
                IsOpen = true,
                IsVisible = true,
                MaxPlayers = MAX_PLAYERS,
                PublishUserId = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { ROOM_TYPE, RoomType.Match } },
                CustomRoomPropertiesForLobby = new[] { ROOM_TYPE }
            };
        }
    }
}
