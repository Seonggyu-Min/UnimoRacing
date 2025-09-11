using Firebase;
using Firebase.Auth;
using Google;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class GoogleSignManager : Singleton<GoogleSignManager>
    {
        [Header("Google Sign-In")]
        [SerializeField] private string _webClientId = "683777357075-3489b5bgoi1mdcka78pvlecs4cg7ftod.apps.googleusercontent.com";
        [SerializeField] private bool _requestEmail = true;

        public event Action<FirebaseUser> OnSignInSucceeded;
        public event Action<string> OnSignInFailed;

        private bool _configured;

        private void Awake()
        {
            SingletonInit();
        }

        private void OnEnable()
        {
            // Firebase 준비되면 설정
            if (FirebaseManager.Instance != null)
            {
                if (FirebaseManager.Instance.IsReady) ConfigureGoogleSignInOnce();
                else FirebaseManager.Instance.OnFirebaseReady += ConfigureGoogleSignInOnce;
            }
        }

        private void OnDisable()
        {
            if (FirebaseManager.Instance != null)
                FirebaseManager.Instance.OnFirebaseReady -= ConfigureGoogleSignInOnce;
        }

        private void ConfigureGoogleSignInOnce()
        {
            if (_configured) return;

            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId = _webClientId,
                RequestIdToken = true,
                RequestEmail = _requestEmail,
                UseGameSignIn = false,
                ForceTokenRefresh = false
            };
            _configured = true;
            Debug.Log("[GoogleAuth] Configured");
        }

        // 버튼과 연결하여 Google Sign-In 시작
        public void SignInWithGoogle()
        {
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsReady)
            {
                //Fail("Firebase not ready yet.");
                // 파이어 베이스가 초기화되지 않은 상황이긴 한데, 이걸 유저한테 직접 말할 수는 없을 듯
                Fail("앱 초기화가 완료되지 않았습니다. 네트워크 상태를 확인한 뒤 앱을 다시 시작해 주세요.");
                return;
            }
            if (!_configured) ConfigureGoogleSignInOnce();

            SignInFlow().Forget();
        }

        public void SilentSignIn()
        {
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsReady) return;
            if (!_configured) ConfigureGoogleSignInOnce();

            Debug.Log("[GoogleAuth] Attempting silent sign-in...");

            SilentSignInFlow().Forget();
        }

        public void SignOut()
        {
            try
            {
                FirebaseManager.Instance?.Auth?.SignOut();
                GoogleSignIn.DefaultInstance.SignOut();
                Debug.Log("[GoogleAuth] Signed out");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GoogleAuth] SignOut warning: {e.Message}");
            }
        }

        private async Task SignInFlow()
        {
            try
            {
                var gUser = await GoogleSignIn.DefaultInstance.SignIn(); // 네이티브 UI 활성화
                if (gUser == null)
                {
                    // Fail("User is null (cancelled?)"); 
                    //Fail("로그인이 취소되었어요.");
                    return; 
                }

                await SignInToFirebase(gUser);
            }
            catch (Exception e)
            {
                //GoogleSignIn.SignInException sie = null;

                //if (e is GoogleSignIn.SignInException se) sie = se;
                //else if (e.InnerException is GoogleSignIn.SignInException se2) sie = se2;

                //if (sie != null)
                //    Fail($"Google Sign-In failed: {sie.Status} / {sie.Message}");
                //else
                //    Fail($"Google Sign-In failed: {e.Message}");


                GoogleSignIn.SignInException sie = null;

                if (sie != null)
                {
                    switch (sie.Status.ToString())
                    {
                        case "NETWORK_ERROR":
                            Fail("네트워크 연결이 불안정합니다. 네트워크 상태를 확인한 뒤 다시 시도해 주세요.");
                            break;
                        case "SIGN_IN_CURRENTLY_IN_PROGRESS":
                            Fail("이미 로그인을 진행 중이에요. 잠시만 기다려 주세요.");
                            break;
                        case "DEVELOPER_ERROR":
                            Fail("로그인 설정에 문제가 있습니다. 잠시 후 다시 시도해 주세요.");
                            break;
                        case "SIGN_IN_FAILED":
                            Fail("구글 로그인에 실패했어요. 잠시 후 다시 시도해 주세요.");
                            break;
                        case "INVALID_ACCOUNT":
                            Fail("이 기기에서 사용할 수 없는 계정이에요. 다른 계정으로 시도해 주세요.");
                            break;
                        case "SIGN_IN_REQUIRED":
                            Fail("로그인이 필요합니다. 다시 시도해 주세요.");
                            break;
                        case "TIMEOUT":
                            Fail("응답이 지연되고 있어요. 네트워크 상태 확인 후 다시 시도해 주세요.");
                            break;
                        case "INTERNAL_ERROR":
                            Fail("일시적인 오류가 발생했어요. 잠시 후 다시 시도해 주세요.");
                            break;
                        default:
                            Fail("로그인 중 오류가 발생했어요. 다시 시도해 주세요.");
                            break;
                    }
                }
                else
                {
                    Fail("로그인 중 알 수 없는 오류가 발생했어요. 잠시 후 다시 시도해 주세요.");
                }
            }
        }

        private async Task SilentSignInFlow()
        {
            try
            {
                var gUser = await GoogleSignIn.DefaultInstance.SignInSilently();
                if (gUser == null)
                { 
                    //Fail("Silent sign-in returned null");
                    Fail("자동 로그인 세션이 만료되었습니다. 로그인 버튼으로 다시 로그인해 주세요.");
                    return;
                }

                await SignInToFirebase(gUser);
            }
            catch (Exception e)
            {
                //Fail($"Silent sign-in failed: {e.Message}");
                //Fail("자동 로그인에 실패했습니다. 수동 로그인을 시도해 주세요.");
            }
        }

        private async Task SignInToFirebase(GoogleSignInUser gUser)
        {
            if (string.IsNullOrEmpty(gUser.IdToken))
            {
                //Fail("IdToken is empty. Check WebClientId & provider settings.");
                Fail("로그인에 필요한 인증 정보를 불러오지 못했어요. 앱을 다시 시작한 뒤 로그인해 주세요.");
                return;
            }

            var cred = GoogleAuthProvider.GetCredential(gUser.IdToken, null);
            var user = await FirebaseManager.Instance.Auth.SignInWithCredentialAsync(cred);
            Debug.Log($"[GoogleAuth] Firebase signed in: {user.DisplayName} ({user.UserId})");

            OnSignInSucceeded?.Invoke(user);
        }

        private void Fail(string msg)
        {
            Debug.LogWarning($"[GoogleAuth] {msg}");
            OnSignInFailed?.Invoke(msg);
        }
    }

    internal static class TaskExt
    {
        public static async void Forget(this Task task)
        {
            try { await task; } catch (Exception e) { Debug.LogException(e); }
        }
    }
}
