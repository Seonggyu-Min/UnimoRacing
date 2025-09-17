using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace MSG
{
    public class FriendRequestPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _infoText;
        private string _pairId;
        private string _toUid;
        private string _fromUid;

        public void Init(string pairId, string toUid, string fromUid)
        {
            _pairId = pairId;
            _toUid = toUid;
            _fromUid = fromUid;

            // 닉네임 비동기 로드라서 미리 텍스트 설정
            _infoText.text = "요청 로딩 중…";

            DatabaseManager.Instance.GetOnMain(DBRoutes.Nickname(_fromUid),
                snap => _infoText.text = $"{snap.Value}",
                err => Debug.LogWarning($"친구 요청 로딩 에러: {err}")
            );
        }

        public void OnClickAccecpt()
        {
            FriendsLogics.Instance.AcceptRequest(
                _pairId,
                _toUid,
                () => Debug.Log("친구 수락 성공"),
                err => Debug.LogWarning($"{err}")
                );
        }

        public void OnClickReject()
        {
            FriendsLogics.Instance.RejectRequest(
                _pairId,
                _toUid,
                () => Debug.Log("친구 거절 성공"),
                err => Debug.LogWarning($"{err}")
                );
        }
    }
}
