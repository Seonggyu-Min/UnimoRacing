using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Threading.Tasks;

namespace YTW
{
    public class TestNetworkManager : MonoBehaviourPunCallbacks
    {
        [Header("미리 로드할 네트워크 프리팹 라벨")]
        [SerializeField] private string _networkPrefabLabel = "NetworkPrefab"; // 라벨 이름

        void Start()
        {
            _ = Initialize();
        }

        private async Task Initialize()
        {
            // NetworkAssetLoader에게 라벨을 전달하여 프리팹들을 미리 로드
            await NetworkAssetLoader.Instance.InitializeAndPreloadAsync(_networkPrefabLabel);

            Debug.Log("[NetworkManager] 에셋 준비 완료. 포톤 서버에 연결을 시도합니다.");
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
