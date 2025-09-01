using MSG;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class PhotonStateDebugger : MonoBehaviour
{
    [SerializeField] private bool _logToConsole = true;
    [SerializeField] private TMP_Text _uiText;

    private void Update()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"[State] {PhotonNetwork.NetworkClientState}");

        if (PhotonNetwork.InLobby)
        {
            sb.AppendLine("현재: 로비 안에 있음");
        }
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            Room room = PhotonNetwork.CurrentRoom;
            sb.AppendLine($"현재: 룸 안에 있음");
            sb.AppendLine($"룸 이름: {room.Name}");
            sb.AppendLine($"룸 인원: {room.PlayerCount}/{room.MaxPlayers}");

            foreach (var kvp in room.Players)
            {
                Player p = kvp.Value;
                sb.AppendLine($"- Player {p.ActorNumber} | UserId={p.UserId} | Nick={p.NickName}");
            }

            sb.AppendLine($"Is Visible: {room.IsVisible}, Is Open: {room.IsOpen}");

            if (room.CustomProperties != null && room.CustomProperties.TryGetValue(RoomMakeHelper.ROOM_TYPE, out object rt))
            {
                sb.AppendLine($"RoomType Property: Room Type: ({(RoomType)rt})");
            }
            else
            {
                sb.AppendLine("RoomType Property: 없음");
            }
        }
        else
        {
            sb.AppendLine("룸 안에 있지 않음");
        }

        string msg = sb.ToString();

        if (_logToConsole)
        {
            Debug.Log(msg);
        }
        if (_uiText != null)
        {
            _uiText.text = msg;
        }
    }
}
