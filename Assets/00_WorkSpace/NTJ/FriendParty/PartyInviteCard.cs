using MSG;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//public class PartyInviteCard : MonoBehaviour
//{
//    [SerializeField] private Image _backgroundImage;
//    [SerializeField] private Sprite _inviteBackgroundSprite;
//    [SerializeField] private TMP_Text _nicknameText;
//    [SerializeField] private TMP_Text _levelText;
//    [SerializeField] private Image _unimoIcon;
//
//    [SerializeField] private Button _acceptButton;
//    [SerializeField] private Button _rejectButton;
//
//    private ChatDM _chatDM;
//    private PartyInviteMsg _currentInviteMsg;
//
//    // 이 함수는 FriendSubscriber에서 호출됩니다.
//    public void Init(PartyInviteMsg inviteMsg, ChatDM chat)
//    {
//        _currentInviteMsg = inviteMsg;
//        _chatDM = chat;
//
//        // 배경 이미지를 초록색 스프라이트로 변경
//        if (_inviteBackgroundSprite != null)
//        {
//            _backgroundImage.sprite = _inviteBackgroundSprite;
//        }
//
//        // 플레이어 정보 로드 및 표시
//        DatabaseManager.Instance.GetOnMain(
//            DBRoutes.Users(inviteMsg.leaderUid),
//            snap => {
//                string nickname = snap.Child(DatabaseKeys.nickname).Value.ToString();
//                int exp = int.Parse(snap.Child(DatabaseKeys.gameData).Child(DatabaseKeys.experience).Value.ToString());
//                int level = ExpToLevel.LevelFromTotalExp(exp);
//                int equippedIndex = int.Parse(snap.Child(DatabaseKeys.equipped).Child(DatabaseKeys.unimos).Value.ToString());
//
//                _nicknameText.text = nickname;
//                _levelText.text = $"Lv {level}";
//
//                if (UnimoKartDatabase.Instance.TryGetByUnimoIndex(equippedIndex, out UnimoCharacterSO unimo) && unimo != null)
//                {
//                    _unimoIcon.sprite = unimo.characterSprite;
//                }
//            }
//        );
//
//        // 버튼 클릭 이벤트 연결
//        _acceptButton.onClick.AddListener(OnAccept);
//        _rejectButton.onClick.AddListener(OnReject);
//    }
//
//    private void OnAccept()
//    {
//        string currentUid = FirebaseManager.Instance.Auth.CurrentUser.UserId;
//        _chatDM.SendPartyAccept(_currentInviteMsg.leaderUid, _currentInviteMsg.partyId, currentUid);
//
//        // UI 초기화
//        PartyInviteManager.Instance.ClearPendingInvite();
//    }
//
//    private void OnReject()
//    {
//        string currentUid = FirebaseManager.Instance.Auth.CurrentUser.UserId;
//        _chatDM.SendPartyReject(_currentInviteMsg.leaderUid, _currentInviteMsg.partyId, currentUid, "거절");
//
//        // UI 초기화
//        PartyInviteManager.Instance.ClearPendingInvite();
//    }
//}
