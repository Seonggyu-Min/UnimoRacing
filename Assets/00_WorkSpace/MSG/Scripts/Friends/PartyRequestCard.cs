using MSG;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace MSG
{
    public class PartyRequestCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nicknameText;

        private ChatDM _chat;
        private string _targetUid;
        private PartyService _party;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;


        public void Init(string uid, ChatDM chat, PartyService party)
        {
            _targetUid = uid;
            _chat = chat;
            _party = party;

            DatabaseManager.Instance.GetOnMain(DBRoutes.Nickname(_targetUid),
                snap => _nicknameText.text = $"{snap.Value}",
                err => Debug.LogWarning($"현재 닉네임 읽기 오류: {err}")
                );
        }

        public void OnClickPartyRequest()
        {
            if (!_party.IsLeader)
            {
                Debug.Log("리더만 초대할 수 있습니다.");
                return;
            }

            if (_party.Members.Contains(_targetUid))
            {
                Debug.Log("이미 파티에 있는 인원입니다");
                return;
            }

            if (!_party.IsInParty) _party.SetParty(CurrentUid, new List<string>()); // 파티에 없는 솔로 상태면 상태 전환

            _party.EnsurePartyIdForLeader(CurrentUid); // 파티 아이디 없을까봐 생성

            string partyId = _party.CurrentPartyId;
            string leaderUid = _party.LeaderUid;
            string[] members = _party.Members.ToArray();

            _chat.SendPartyInvite(_targetUid, partyId, leaderUid, members);

            Debug.Log($"{_targetUid}에게 {partyId}로 초대 완료");
        }
    }
}
