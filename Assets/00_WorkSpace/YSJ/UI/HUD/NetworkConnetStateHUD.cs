using Photon.Pun;
using TMPro;
using UnityEngine;

public class NetworkConnetStateHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _networkClientStateTMP;
    [SerializeField] private TextMeshProUGUI _localPlayerTMP;
    [SerializeField] private TextMeshProUGUI _currentRoomTMP;

    private void Update()
    {
        if (_networkClientStateTMP != null)
            _networkClientStateTMP.text = $"Network Client State: {PhotonNetwork.NetworkClientState}";

        if (_localPlayerTMP != null)
            _localPlayerTMP.text = $"Local Player: {PhotonNetwork.LocalPlayer}";

        if (PhotonNetwork.InRoom)
        {
            if (_currentRoomTMP != null)
            {
                _currentRoomTMP.text = $"Current Room: {PhotonNetwork.CurrentRoom} \n" +
                    $"Current Room Count: {PhotonNetwork.CountOfRooms}\n" +
                    $"Current Room CountOfPlayersInRooms: {PhotonNetwork.CountOfPlayersInRooms}\n" +
                    $"Current Room CountOfPlayersOnMaster: {PhotonNetwork.CountOfPlayersOnMaster}\n" +
                    $"Current Room MaxPlayer: {PhotonNetwork.CurrentRoom.MaxPlayers}\n" +
                    $"{RoomManager.Instance?.PrintCustomProperties("NetworkConnetStateHUD.Update")}" +
                    $"{RoomManager.Instance?.PrintCurrentPlayers()}";
            }
        }
    }
}
