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
        [SerializeField] private GameObject _infoTextObj;
        [SerializeField] private TMP_Text _infoText;
        [SerializeField] private Button _sendButton;

        private bool _isSent;
        private Coroutine _infoTextCO;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;


        private void OnDisable()
        {
            StopInfoCO();
        }


        public void OnClickSend()
        {
            if (_isSent) return;
            _isSent = true;

            if (string.IsNullOrEmpty(_nicknameInputField.text))
            {
                StartInfoCO("닉네임을 입력해주세요!");
                _isSent = false;
                return;
            }

            // 먼저 닉네임 기반 uid 조회
            string nickname = _nicknameInputField.text;
            string toUid = string.Empty;

            DatabaseManager.Instance.GetOnMain(DBRoutes.Nicknames(nickname),
                snap =>
                {
                    toUid = snap?.Value?.ToString();

                    if (string.IsNullOrEmpty(toUid))
                    {
                        StartInfoCO("해당 닉네임을 가진 유저가 없습니다!");
                        _isSent = false;
                        return;
                    }

                    // 그리고 해당 uid 기반으로 친구 요청

                    _friendLogics.SendRequest(CurrentUid, toUid,
                        () =>
                        {
                            StartInfoCO("친구 요청을 보냈습니다!");
                            _isSent = false;
                        },
                        err =>
                        {
                            StartInfoCO("친구 요청에 실패했습니다!");
                            Debug.LogWarning($"친구 요청 작업 실패: {err}");
                            _isSent = false;
                        });
                },
                err =>
                {
                    StartInfoCO("친구 요청에 실패했습니다!");
                    Debug.LogWarning($"현재 uid 읽기 오류 {err}");
                    _isSent = false;
                    return;
                });
        }

        private void StartInfoCO(string msg)
        {
            if (_infoTextCO != null)
            {
                StopCoroutine(_infoTextCO);
                _infoTextCO = null;
                _infoText.text = string.Empty;
                _infoTextObj.SetActive(false);
            }

            _infoTextCO = StartCoroutine(InfoRoutine(msg));
        }

        private void StopInfoCO()
        {
            if (_infoTextCO != null)
            {
                StopCoroutine(_infoTextCO);
                _infoTextCO = null;
                _infoText.text = string.Empty;
                _infoTextObj.SetActive(false);
            }
        }


        private IEnumerator InfoRoutine(string msg)
        {
            _infoText.text = msg;
            _infoTextObj.SetActive(true);

            yield return new WaitForSeconds(3f);

            _infoText.text = string.Empty;
            _infoTextObj.SetActive(false);
        }
    }
}
