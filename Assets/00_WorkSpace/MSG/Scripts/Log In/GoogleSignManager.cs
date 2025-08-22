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
                Fail("Firebase not ready yet.");
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
                if (gUser == null) { Fail("User is null (cancelled?)"); return; }

                await SignInToFirebase(gUser);
            }
            catch (Exception e)
            {
                GoogleSignIn.SignInException sie = null;

                if (e is GoogleSignIn.SignInException se) sie = se;
                else if (e.InnerException is GoogleSignIn.SignInException se2) sie = se2;

                if (sie != null)
                    Fail($"Google Sign-In failed: {sie.Status} / {sie.Message}");
                else
                    Fail($"Google Sign-In failed: {e.Message}");
            }
        }

        private async Task SilentSignInFlow()
        {
            try
            {
                var gUser = await GoogleSignIn.DefaultInstance.SignInSilently();
                if (gUser == null) { Fail("Silent sign-in returned null"); return; }

                await SignInToFirebase(gUser);
            }
            catch (Exception e)
            {
                Fail($"Silent sign-in failed: {e.Message}");
            }
        }

        private async Task SignInToFirebase(GoogleSignInUser gUser)
        {
            if (string.IsNullOrEmpty(gUser.IdToken))
            {
                Fail("IdToken is empty. Check WebClientId & provider settings.");
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
