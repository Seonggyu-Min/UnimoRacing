using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace MSG
{
    public class VotePanelBehaviour : MonoBehaviourPunCallbacks
    {
        [SerializeField] private TMP_Text _countDownText;

        private double _endsAt = -1;

        public override void OnEnable()
        {
            base.OnEnable();
            TryFetchEndsAt();
        }

        public override void OnRoomPropertiesUpdate(Hashtable changedProps)
        {
            if (changedProps.ContainsKey(PhotonNetworkCustomProperties.KEY_ROOM_VOTE_END_AT))
            {
                TryFetchEndsAt();
            }
        }

        private void TryFetchEndsAt()
        {
            if (PhotonNetworkCustomProperties.TryGetRoomProp(
                RoomKey.VoteEndTime,
                out double time
                ))
            {
                _endsAt = time;
            }
        }

        private void Update()
        {
            if (_endsAt < 0) return;

            double remain = _endsAt - PhotonNetwork.Time;
            if (remain < 0) remain = 0;

            _countDownText.text = Mathf.CeilToInt((float)remain).ToString();
        }
    }
}
