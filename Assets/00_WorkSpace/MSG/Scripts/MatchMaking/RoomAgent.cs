using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace MSG
{
    public class RoomAgent : MonoBehaviour
    {
        public async Task EnsureHomeRoomAsync(string roomName)
        {
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.Name == roomName)
                return;

            await JoinOrCreateAsync(roomName, MakeHomeOptions());
        }

        public async Task JoinOrCreateAsync(string roomName, RoomOptions options)
        {
            if (PhotonNetwork.InRoom) 
            {
                PhotonNetwork.LeaveRoom();
            }

            await WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer);
            PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
            await WaitUntil(() => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.Name == roomName);
            await LeaveIfOverCapacityAsync();
        }

        public async Task<bool> TryJoinRandomWithSlotAsync(int minFreeSlots)
        {
            if (PhotonNetwork.InRoom) 
            {
                PhotonNetwork.LeaveRoom();
            }
            await WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer);

            ExitGames.Client.Photon.Hashtable expectedProps = new ExitGames.Client.Photon.Hashtable();

            PhotonNetwork.JoinRandomRoom(expectedProps, 0);
            bool joined = await WaitUntilOrTimeout(() => PhotonNetwork.InRoom, 2000);
            if (!joined) return false;

            int free = PhotonNetwork.CurrentRoom.MaxPlayers - PhotonNetwork.CurrentRoom.PlayerCount;
            if (free >= minFreeSlots)
            {
                await LeaveIfOverCapacityAsync();
                return true;
            }

            PhotonNetwork.LeaveRoom();
            await WaitUntil(() => !PhotonNetwork.InRoom);
            return false;
        }

        public async Task LeaveIfOverCapacityAsync()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

            int max = PhotonNetwork.CurrentRoom.MaxPlayers;
            int count = PhotonNetwork.CurrentRoom.PlayerCount;

            if (count > max)
            {
                PhotonNetwork.LeaveRoom();
                await WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer);
            }
        }

        private RoomOptions MakeHomeOptions()
        {
            RoomOptions opts = new RoomOptions();
            opts.IsOpen = true;
            opts.IsVisible = true;
            opts.MaxPlayers = RoomMakeHelper.MAX_PLAYERS;
            return opts;
        }

        private async Task WaitUntil(Func<bool> predicate)
        {
            while (!predicate())
            {
                await Task.Yield(); 
            }
        }

        private async Task<bool> WaitUntilOrTimeout(Func<bool> predicate, int ms)
        {
            DateTime end = DateTime.UtcNow.AddMilliseconds(ms);
            while (DateTime.UtcNow < end)
            {
                if (predicate()) return true;
                await Task.Yield();
            }
            return false;
        }
    }
}
