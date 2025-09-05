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
        [SerializeField] private TMP_Text _levelText;

        private ChatDM _chat;
        private string _targetUid;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;


        public void Init(string uid, ChatDM chat)
        {
            _targetUid = uid;
            _chat = chat;

            DatabaseManager.Instance.GetOnMain(
                DBRoutes.Users(_targetUid),
                snap =>
                {
                    string nickname = "";
                    var nickSnap = snap.Child(DatabaseKeys.nickname);
                    if (nickSnap.Exists && nickSnap.Value != null)
                    {
                        nickname = nickSnap.Value.ToString();
                    }
                    _nicknameText.text = nickname;

                    int level = 1;
                    var expSnap = snap.Child(DatabaseKeys.gameData).Child(DatabaseKeys.experience);
                    if (expSnap.Exists && expSnap.Value != null)
                    {
                        long exp = 0;
                        long.TryParse(expSnap.Value.ToString(), out exp);
                        level = ExpToLevel.Convert((int)exp);
                    }
                    _levelText.text = $"lv {level}";
                    Debug.Log($"{nickname}의 레벨: {level}");
                },
                err => Debug.LogWarning($"[PartyRequestCard] 사용자 정보 읽기 오류: {err}")
                );
        }

        public void OnClickPartyRequest()
        {
            if (!PartyService.Instance.IsLeader)
            {
                Debug.Log("리더만 초대할 수 있습니다.");
                return;
            }

            if (PartyService.Instance.Members.Contains(_targetUid))
            {
                Debug.Log("이미 파티에 있는 인원입니다");
                return;
            }

            if (!PartyService.Instance.IsInParty) PartyService.Instance.SetParty(CurrentUid, new List<string>()); // 파티에 없는 솔로 상태면 상태 전환

            PartyService.Instance.EnsurePartyIdForLeader(CurrentUid); // 파티 아이디 없을까봐 생성

            string partyId = PartyService.Instance.CurrentPartyId;
            string leaderUid = PartyService.Instance.LeaderUid;
            string[] members = PartyService.Instance.Members.ToArray();

            _chat.SendPartyInvite(_targetUid, partyId, leaderUid, members);

            Debug.Log($"{_targetUid}에게 {partyId}로 초대 완료");
        }
    }
}
