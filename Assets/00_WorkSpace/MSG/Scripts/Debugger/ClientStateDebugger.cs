using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


namespace MSG
{
    public class ClientStateDebugger : MonoBehaviour
    {
        [SerializeField] private TMP_Text _debugText;


        private void Update()
        {
            var state = PhotonNetwork.NetworkClientState;
            var inRoom = PhotonNetwork.InRoom;

            if (inRoom && PhotonNetwork.CurrentRoom != null)
            {
                var room = PhotonNetwork.CurrentRoom;
                // UserId가 null일 수 있으니 fallback 준비
                var ids = PhotonNetwork.PlayerList
                    .Select(p => !string.IsNullOrEmpty(p.UserId) ? p.UserId
                            : (!string.IsNullOrEmpty(p.NickName) ? p.NickName
                            : $"Actor#{p.ActorNumber}"));

                _debugText.text =
                    $"ClientState : {state}\n" +
                    $"InRoom      : {inRoom}\n" +
                    $"Room        : {room.Name} ({room.PlayerCount}/{room.MaxPlayers})  Open={room.IsOpen} Visible={room.IsVisible}\n" +
                    $"Players     : {string.Join(", ", ids)}";
            }
            else
            {
                _debugText.text =
                    $"ClientState : {state}\n" +
                    $"InRoom      : {inRoom}\n" +
                    $"Room        : (Not in room)\n" +
                    $"Players     : -";
            }
        }
    }
}
