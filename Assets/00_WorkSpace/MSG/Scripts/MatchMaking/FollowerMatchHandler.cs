using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace MSG
{
    public class FollowerMatchHandler : MonoBehaviour
    {
        [SerializeField] private RoomAgent _room;

        public async Task JoinInviteAsync(string roomName)
        {
            await _room.JoinOrCreateAsync(roomName, CandidateOptions());
        }

        public async Task LeaveNowAsync()
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
                await WaitUntil(() => !Photon.Pun.PhotonNetwork.InRoom);
            }
        }

        public async Task ReturnHomeAsync(string homeRoom)
        {
            await _room.EnsureHomeRoomAsync(homeRoom);
        }

        private static RoomOptions CandidateOptions()
        {
            RoomOptions opts = new RoomOptions();
            opts.PublishUserId = true;
            opts.IsOpen = true;
            opts.IsVisible = true;
            opts.MaxPlayers = RoomMakeHelper.MAX_PLAYERS;
            return opts;
        }

        private static async Task WaitUntil(Func<bool> predicate)
        {
            while (!predicate())
            {
                await Task.Yield();
            }
        }
    }
}
