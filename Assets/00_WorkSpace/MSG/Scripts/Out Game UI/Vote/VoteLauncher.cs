using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace MSG
{
    public class VoteLauncher : MonoBehaviourPunCallbacks
    {
        public override void OnJoinedRoom()
        {
            TrySyncPanelWithRoomState();
        }

        public override void OnRoomPropertiesUpdate(Hashtable changedProps)
        {
            if (changedProps.ContainsKey(PhotonNetworkCustomProperties.KEY_ROOM_VOTE_STATE))
            {
                Debug.Log($"KEY_ROOM_VOTE_STATE 변경 공지 받음");
                ApplyByState(GetState());
            }
        }

        private void TrySyncPanelWithRoomState()
        {
            ApplyByState(GetState());
        }

        private string GetState()
        {
            if (PhotonNetworkCustomProperties.TryGetRoomProp(
                RoomKey.VoteState,
                out string state
                ))
            {
                Debug.Log($"KEY_ROOM_VOTE_STATE: {state}로 변경 공지 받음");
                return state;
            }
            Debug.Log($"KEY_ROOM_VOTE_STATE: {RoomStateKeys.inactive}로 변경 공지 받음");
            return RoomStateKeys.inactive;
        }

        private void ApplyByState(string state)
        {
            if (state == RoomStateKeys.active)
            {
                Debug.Log("투표 패널 활성화 호출됨");
                UIManager.Instance.Show("Vote Panel");
                PlayerManager.Instance.SetPlayerCPVote(-1);
            }
            else
            {
                if (UIManager.Instance.GetUnit("Vote Panel").gameObject.activeSelf)
                {
                    UIManager.Instance.Hide("Vote Panel");
                }
            }
        }
    }
}
