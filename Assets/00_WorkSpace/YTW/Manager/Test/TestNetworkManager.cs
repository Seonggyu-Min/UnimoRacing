using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace YTW
{
    public class TestNetworkManager : MonoBehaviourPunCallbacks
    {
        void Start()
        {
            Debug.Log("[NetworkManager] 포톤 서버에 연결을 시도합니다...");
            // 게임 버전을 설정하고 포톤 서버에 접속합니다.
            PhotonNetwork.GameVersion = "1";
            PhotonNetwork.ConnectUsingSettings();
        }
        public override void OnConnectedToMaster()
        {
            Debug.Log("[NetworkManager] 마스터 서버에 성공적으로 접속했습니다.");
            PhotonNetwork.JoinOrCreateRoom("TestRoom", new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[NetworkManager] '{PhotonNetwork.CurrentRoom.Name}' 방에 성공적으로 참가했습니다.");
            Debug.Log($"현재 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}");

            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[NetworkManager] 당신은 마스터 클라이언트입니다. 씬을 로드할 수 있습니다.");
            }
            else
            {
                Debug.Log("[NetworkManager] 당신은 일반 클라이언트입니다.");
            }
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[NetworkManager] 방 참가 실패: {message} (코드: {returnCode})");
        }
    }

}
