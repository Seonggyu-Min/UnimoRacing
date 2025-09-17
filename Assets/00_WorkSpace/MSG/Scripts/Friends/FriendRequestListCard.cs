using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace MSG
{
    // 내가 보낸 친구 요청 목록에서 보여줄 개별 UI의 컴포넌트
    public class FriendRequestListCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nicknameText;
        [SerializeField] private GameObject _cancelButton;

        private FriendsLogics _friendsLogics;
        private string _pairId;
        private string _otherUid;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;

        public void Init(string pairId, string otherUid, FriendsLogics friendsLogics)
        {
            _pairId = pairId;
            _otherUid = otherUid;
            _friendsLogics = friendsLogics;

            // 닉네임 조회
            DatabaseManager.Instance.GetOnMain(
                DBRoutes.Nickname(_otherUid),
                snap => _nicknameText.text = snap?.Value?.ToString() ?? "알 수 없음",
                err => _nicknameText.text = "닉네임 오류"
            );
        }

        // 취소 기능
        public void OnClickCancelRequest()
        {
            if (_friendsLogics == null)
            {
                Debug.LogWarning("[FriendRequestListCard] FriendsLogics가 null");
                return;
            }

            // 버튼 다중 클릭 방지
            if (_cancelButton) _cancelButton.SetActive(false);

            _friendsLogics.CancelRequest(
                _pairId,
                CurrentUid,
                () => Debug.Log($"[FriendRequestListCard] 취소 성공: {_pairId}"),
                err =>
                {
                    Debug.LogError($"[FriendRequestListCard] 취소 실패: {err}");
                    if (_cancelButton)
                    {
                        _cancelButton.SetActive(true);
                    }
                }
            );
        }
    }
}
