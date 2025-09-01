using MSG.Deprecated;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace MSG
{
    public class PartyJoinPanel : MonoBehaviour
    {
        [SerializeField] private PartyService _party;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private ChatDM _chat;

        private PartyInviteMsg _msg;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;


        public void Init(string senderUid, string payloadJson)
        {
            _msg = JsonUtility.FromJson<PartyInviteMsg>(payloadJson);

            string nickname = "Error";

            DatabaseManager.Instance.GetOnMain(DBRoutes.Nickname(senderUid),
                snap => nickname = $"{snap.Value}",
                err => Debug.LogWarning($"현재 닉네임 읽기 오류: {err}")
                );

            _messageText.text = $"{nickname}님이 파티 초대를 하였습니다. 수락하시겠습니까?";
        }

        public void OnClickAccept()
        {
            _chat.SendPartyAccept(_msg.leaderUid, _msg.partyId, CurrentUid);
            gameObject.SetActive(false);
        }

        public void OnClickReject()
        {
            _chat.SendPartyReject(_msg.leaderUid, _msg.partyId, CurrentUid);
            gameObject.SetActive(false);
        }
    }
}
