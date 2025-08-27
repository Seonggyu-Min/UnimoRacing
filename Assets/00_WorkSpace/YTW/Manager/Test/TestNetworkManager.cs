using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace YTW
{
    public class TestNetworkManager : MonoBehaviourPunCallbacks
    {
        void Start()
        {
            Debug.Log("[NetworkManager] ���� ������ ������ �õ��մϴ�...");
            // ���� ������ �����ϰ� ���� ������ �����մϴ�.
            PhotonNetwork.GameVersion = "1";
            PhotonNetwork.ConnectUsingSettings();
        }
        public override void OnConnectedToMaster()
        {
            Debug.Log("[NetworkManager] ������ ������ ���������� �����߽��ϴ�.");
            PhotonNetwork.JoinOrCreateRoom("TestRoom", new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[NetworkManager] '{PhotonNetwork.CurrentRoom.Name}' �濡 ���������� �����߽��ϴ�.");
            Debug.Log($"���� �÷��̾� ��: {PhotonNetwork.CurrentRoom.PlayerCount}");

            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[NetworkManager] ����� ������ Ŭ���̾�Ʈ�Դϴ�. ���� �ε��� �� �ֽ��ϴ�.");
            }
            else
            {
                Debug.Log("[NetworkManager] ����� �Ϲ� Ŭ���̾�Ʈ�Դϴ�.");
            }
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[NetworkManager] �� ���� ����: {message} (�ڵ�: {returnCode})");
        }
    }

}
