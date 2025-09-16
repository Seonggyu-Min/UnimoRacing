#if UNITY_EDITOR || UNITY_STANDALONE_WIN
#define UNITY_DEV_MODE
#endif

using Firebase.Auth;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;


namespace MSG
{
    public class DevModeLogInHelper : MonoBehaviour
    {
#if UNITY_DEV_MODE
        [SerializeField] private GameObject _devUIs;
        [SerializeField] private GameObject _notDevUIs;
        [SerializeField] private TMP_InputField _emailInputField;
        [SerializeField] private TMP_InputField _passwordInputField;
#endif
        private void Awake()
        {
#if UNITY_DEV_MODE
            _devUIs.SetActive(true);
            _notDevUIs.SetActive(false);
#endif
        }

        public void OnClickLogInOrCreateButton()
        {
#if UNITY_DEV_MODE
            var email = _emailInputField ? _emailInputField.text.Trim() : "";
            var password = _passwordInputField ? _passwordInputField.text : "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                Debug.LogWarning("[DevLogin] 이메일/비밀번호를 입력하세요.");
                return;
            }

            Debug.Log("[DevLogin] 이메일 로그인 시도…");
            SignInOrCreate(email, password).Forget();
#endif
        }

        private async Task SignInOrCreate(string email, string password)
        {
#if UNITY_DEV_MODE
            Debug.Log("[DevLogin] SignInOrCreate 시작");
            try
            {
                var auth = FirebaseManager.Instance.Auth;

                // 로그인 시도
                AuthResult signInResult = await auth.SignInWithEmailAndPasswordAsync(email, password);
                FirebaseUser user = signInResult.User;

                // 성공 처리
                OnDevSignedIn(user);
            }
            catch (Exception)
            {
                try
                {
                    var auth = FirebaseManager.Instance.Auth;

                    // 없으면 가입 및 로그인
                    AuthResult createResult = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
                    FirebaseUser user = createResult.User;

                    OnDevSignedIn(user);
                }
                catch (Exception createEx)
                {
                    Debug.LogWarning($"[DevLogin] 실패: {createEx.Message}");
                }
            }
#endif
        }

        private void OnDevSignedIn(FirebaseUser user)
        {
#if UNITY_DEV_MODE
            Debug.Log($"[DevLogin] 파이어베이스 로그인 상태: 이메일: {user.Email} UID: {user.UserId}");

            PhotonNetwork.AuthValues = new AuthenticationValues(user.UserId);

            var flow = FindAnyObjectByType<AuthFlowController>();
            flow?.SendMessage("HandleSignInSucceeded", user, SendMessageOptions.DontRequireReceiver);
#endif
        }
    }
}
