using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace MSG
{
    public class InGameUIIndicator : MonoBehaviourPunCallbacks
    {
        [SerializeField] private TMP_Text _lapText;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _timtText;

        private readonly string fisrt  = "1st";
        private readonly string second = "2nd";
        private readonly string third  = "3rd";
        private readonly string fourth = "4th";

        private bool _isSetTime = false;

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // 여기서 플레이어 랭크 변동

            // 여기서 랩 수 변동 체크
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            // 여기서 룸 프로퍼티 (시작시간 RoomKey.RaceStartTime) 받아와서
            // PhotonNetwork.Time - (시작시간) = 현재 달리는 시간
            // _isSetTime = true;
        }

        private void Update()
        {
            if (!_isSetTime) return;
            // 그리고 KEY_PLAYER_RACE_IS_FINISHED가 null이 아니게 되면 return (완주 했다는 뜻)

            // 그리고 시간 표기 텍스트 업데이트
        }
    }
}
