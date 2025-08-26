using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace MSG
{
    public class NicknameSetter : MonoBehaviour
    {
        [SerializeField] private AuthFlowController _authFlowController;
        [SerializeField] private GameObject _nicknameObj; // 끈 상태로 시작
        [SerializeField] private TMP_Text _infoText;
        [SerializeField] private TMP_InputField _nicknameInputField;


        private void Start()
        {
            _authFlowController.OnAuthSucceeded += CheckNicknameSet;
        }

        private void OnDisable()
        {
            if (_authFlowController != null)
            {
                _authFlowController.OnAuthSucceeded -= CheckNicknameSet;
            }
        }

        private void CheckNicknameSet()
        {
            DatabaseManager.Instance.GetOnMain((DBRoutes.Nickname(FirebaseManager.Instance.Auth.CurrentUser.UserId)), snap =>
            {
                if (!snap.Exists) // 닉네임이 없으면
                {
                    _nicknameObj.SetActive(true);
                }
            });
        }

        public void OnClickSetNickName()
        {
            string newNickname = _nicknameInputField.text;

            DatabaseManager.Instance.GetOnMain(DBRoutes.Nicknames(newNickname), snap =>
            {
                if (snap.Exists)
                {
                    _infoText.text = $"닉네임: {newNickname}가 이미 존재합니다";
                }
                else
                {
                    var updates = new Dictionary<string, object>
                    {
                        { DBRoutes.Nickname(FirebaseManager.Instance.Auth.CurrentUser.UserId), newNickname}, // users/{uid}/nickname에 자신의 닉네임 설정
                        { DBRoutes.Nicknames(newNickname), FirebaseManager.Instance.Auth.CurrentUser.UserId} // 빠른 조회를 위해 역인덱스로 nicknames/{newNickname}에 uid 설정
                    };

                    DatabaseManager.Instance.UpdateOnMain(updates,
                        onSuccess: () => _infoText.text = $"{newNickname}로 닉네임 설정 완료",
                        onError: err => _infoText.text = $"닉네임 설정 오류: {err}"
                        );
                }
            });
        }
    }
}
