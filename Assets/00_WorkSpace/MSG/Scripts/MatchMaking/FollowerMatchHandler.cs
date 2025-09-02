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
            await _room.JoinOrCreateAsync(roomName, RoomMakeHelper.MakeMatchOptions());
        }

        public async Task LeaveNowAsync()
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
                await WaitUntil(() => !PhotonNetwork.InRoom);
            }
        }

        public async Task ReturnHomeAsync(string homeRoom)
        {
            await _room.EnsureHomeRoomAsync(homeRoom);
        }

        private async Task WaitUntil(Func<bool> predicate)
        {
            while (!predicate())
            {
                await Task.Yield();
            }
        }
    }
}
