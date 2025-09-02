using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class SendRequestFriend : MonoBehaviour
    {
        [SerializeField] private FriendsLogics _friendLogics;

        [SerializeField] private TMP_InputField _nicknameInputField;
        [SerializeField] private TMP_Text _infoText;
        [SerializeField] private Button _sendButton;

        private bool _isSent;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;

        public void OnClickSend()
        {
            if (_isSent) return;
            _isSent = true;

            if (string.IsNullOrEmpty(_nicknameInputField.text))
            {
                _infoText.text = "닉네임을 입력해주세요";
                _isSent = false;
                return;
            }

            // 먼저 닉네임 기반 uid 조회
            string nickname = _nicknameInputField.text;
            string toUid = string.Empty;

            DatabaseManager.Instance.GetOnMain(DBRoutes.Nicknames(nickname),
                snap =>
                {
                    toUid = snap.Value.ToString();

                    if (string.IsNullOrEmpty(toUid))
                    {
                        _infoText.text = $"현재 uid 읽기 오류";
                        _isSent = false;
                        return;
                    }

                    // 그리고 해당 uid 기반으로 친구 요청

                    _friendLogics.SendRequest(CurrentUid, toUid,
                        () =>
                        {
                            _infoText.text = $"친구 요청 작업 완료";
                            _isSent = false;
                        },
                        err =>
                        {
                            _infoText.text = $"친구 요청 작업 실패: {err}";
                            _isSent = false;
                        });
                }, 
                err =>
                {
                    Debug.LogWarning($"현재 uid 읽기 오류 {err}");
                    _isSent = false;
                    return;
                });
        }
    }
}
