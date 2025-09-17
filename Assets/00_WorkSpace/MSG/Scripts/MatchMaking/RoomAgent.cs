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

            Debug.Log($"EnsureHomeRoomAsync 호출됨. 들어가고자 하는 방 이름: {roomName}");

            var opts = roomName.StartsWith("p_")
                ? RoomMakeHelper.MakePartyHomeOptions()
                : RoomMakeHelper.MakeHomeOptions();

            await JoinOrCreateAsync(roomName, opts);
        }

        public async Task JoinOrCreateAsync(string roomName, RoomOptions options)
        {
            Debug.Log($"JoinOrCreateAsync 호출됨. 들어가고자 하는 방 이름: {roomName}");

            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.Name == roomName)
            {
                Debug.Log($"[JoinOrCreateAsync] 이미 목표한 방({PhotonNetwork.CurrentRoom.Name})에 있어 Early Return");
                return;
            }

            if (PhotonNetwork.InRoom)
            {
                Debug.Log($"[JoinOrCreateAsync] 이미 방({PhotonNetwork.CurrentRoom.Name})에 있어 LeaveRoom 호출");
                PhotonNetwork.LeaveRoom();
            }

            Debug.Log("[JoinOrCreateAsync] MasterServer로 복귀 대기 시작");

            await WaitUntil(() =>
            {
                return PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer;
            });

            Debug.Log("[JoinOrCreateAsync] MasterServer로 복귀 완료");

            Debug.Log($"[JoinOrCreateAsync] {roomName}으로 JoinOrCreateRoom 시도");

            PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
            Debug.Log("[JoinOrCreateAsync] 방 입장 대기 시작");
            await WaitUntil(() =>
            {
                //Debug.Log($"[JoinOrCreateAsync] InRoom: {PhotonNetwork.InRoom}, CurrentRoom: {PhotonNetwork.CurrentRoom?.Name}");
                return PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.Name == roomName;
            });
            Debug.Log($"[JoinOrCreateAsync] 방 입장 성공, 방 이름: {PhotonNetwork.CurrentRoom.Name}");

            await LeaveIfOverCapacityAsync();
        }

        public async Task<bool> TryJoinRandomWithSlotAsync(int minFreeSlots)
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
            await WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer);

            var expected = new ExitGames.Client.Photon.Hashtable{ { RoomMakeHelper.ROOM_TYPE, RoomType.Match } };
            PhotonNetwork.JoinRandomRoom(expected, RoomMakeHelper.MAX_PLAYERS);
            bool joined = await WaitUntilOrTimeout(() => PhotonNetwork.InRoom, 3000);
            if (!joined) return false;

            int free = PhotonNetwork.CurrentRoom.MaxPlayers - PhotonNetwork.CurrentRoom.PlayerCount;
            if (free >= minFreeSlots)
            {
                await LeaveIfOverCapacityAsync();
                return true;
            }

            PhotonNetwork.LeaveRoom();
            await WaitUntil(() => !PhotonNetwork.InRoom);
            Debug.Log("남는 방 찾기 실패");
            return false;
        }

        public async Task LeaveIfOverCapacityAsync()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

            int max = PhotonNetwork.CurrentRoom.MaxPlayers;
            int count = PhotonNetwork.CurrentRoom.PlayerCount;

            if (count > max)
            {
                Debug.Log("인원이 다 차서 파티가 못들어와 나가기 시작");
                PhotonNetwork.LeaveRoom();
                await WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer);
            }
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
