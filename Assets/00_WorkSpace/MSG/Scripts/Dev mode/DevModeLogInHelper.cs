using Firebase;
using Firebase.Auth;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace MSG
{
    public class DevModeLogInHelper : MonoBehaviour
    {

        [SerializeField] private GameObject _devUIs;
        [SerializeField] private GameObject _notDevUIs;
        [SerializeField] private TMP_InputField _emailInputField;
        [SerializeField] private TMP_InputField _passwordInputField;

        private void Awake()
        {
            _devUIs.SetActive(true);
            _notDevUIs.SetActive(false);
        }

        public void OnClickLogInButton()
        {
            var email = _emailInputField ? _emailInputField.text.Trim() : "";
            var password = _passwordInputField ? _passwordInputField.text : "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                Debug.LogWarning("[DevLogin] 이메일/비밀번호를 입력하세요.");
                return;
            }

            Debug.Log("[DevLogin] 이메일 로그인 시도…");

            var auth = FirebaseAuth.DefaultInstance;
            // 아직 미구현
        }
    }

}
