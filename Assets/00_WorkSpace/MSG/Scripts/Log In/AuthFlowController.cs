using Firebase.Auth;
using Google;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public enum AuthState 
    {
        Idle, 
        Busy, 
        SignedIn, 
        Failed 
    }
    public enum AttemptKind
    {
        None,
        Silent,
        Interactive 
    }

    public class AuthFlowController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button _googleLoginButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _signOutButton;
        [SerializeField] private GameObject _loadingSpinner;
        [SerializeField] private GameObject _errorPanel;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _errorText;

        [Header("Behaviour")]
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private float _silentDelaySec = 0.1f;
        [SerializeField] private float _silentTimeoutSec = 6f;

        public event Action OnAuthSucceeded;
        public event Action<string> OnAuthFailed;
        public event Action<AuthState> OnAuthStateChanged;

        private AuthState _state = AuthState.Idle;
        private bool _busy;
        private AttemptKind _attempt = AttemptKind.None;
        private Coroutine _timeoutCO;
        private bool _failedHandledOnce;

        private static readonly string CHECKING = "Checking login status…";
        private static readonly string TRY_AUTO = "Trying automatic login…";
        private static readonly string GOOGLE_SIGNIN = "Signing in with Google…";
        private static readonly string SIGNED_OUT = "Signed out. Please log in again.";
        private static readonly string AUTO_FAIL_PROMPT = "Automatic login failed. Please press the Google login button.";
        private static readonly string LOGIN_FAIL = "Login failed.";
        private static readonly string LOGIN_FAIL_NET = "Login failed. Please check your network connection.";
        private static readonly string LOGIN_FAIL_LATER = "Login failed. Please try again later.";
        private static readonly string WELCOME_FMT = "Welcome, {0}!";

        private GoogleSignManager Manager
        {
            get
            {
                if (GoogleSignManager.Instance == null)
                {
                    Debug.LogError("[AuthFlow] GoogleSignManager.Instance is null. Make sure it exists in the scene.");
                }
                return GoogleSignManager.Instance;
            }
        }

        private void Awake()
        {
            if (GoogleSignManager.Instance == null)
            {
                Debug.LogError("[AuthFlow] GoogleSignManager is missing. Disabling AuthFlowController.");
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            if (GoogleSignManager.Instance != null)
            {
                GoogleSignManager.Instance.OnSignInSucceeded += HandleSignInSucceeded;
                GoogleSignManager.Instance.OnSignInFailed += HandleSignInFailed;
            }

            if (_googleLoginButton) _googleLoginButton.onClick.AddListener(OnClickGoogleLogin);
            if (_retryButton) _retryButton.onClick.AddListener(OnClickRetry);
            if (_signOutButton) _signOutButton.onClick.AddListener(OnClickSignOut);

            SetUI(AuthState.Idle, CHECKING, showError: false);
        }

        private void Start()
        {
#if UNITY_EDITOR
            return;
#endif

            if (!_autoStart || !enabled) return;

            if (!FirebaseManager.Instance.IsReady)
            {
                FirebaseManager.Instance.OnFirebaseReady += TrySilentThenInteractive;
                return;
            }

            TrySilentThenInteractive();
        }

        private void OnDisable()
        {
            if (GoogleSignManager.Instance != null)
            {
                GoogleSignManager.Instance.OnSignInSucceeded -= HandleSignInSucceeded;
                GoogleSignManager.Instance.OnSignInFailed -= HandleSignInFailed;
            }
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.OnFirebaseReady -= TrySilentThenInteractive;
            }

            if (_googleLoginButton) _googleLoginButton.onClick.RemoveListener(OnClickGoogleLogin);
            if (_retryButton) _retryButton.onClick.RemoveListener(OnClickRetry);
            if (_signOutButton) _signOutButton.onClick.RemoveListener(OnClickSignOut);

            StopAttemptTimeout();
        }


        public void TrySilentThenInteractive()
        {
            if (!BeginBusy(TRY_AUTO, AttemptKind.Silent)) return;

            _failedHandledOnce = false;
            Manager?.SilentSignIn();
        }

        public void OnClickGoogleLogin()
        {
            if (!BeginBusy(GOOGLE_SIGNIN, AttemptKind.Interactive)) return;

            _failedHandledOnce = false;
            Manager?.SignInWithGoogle();
        }

        public void OnClickSignOut()
        {
            if (_busy) return;
            Manager?.SignOut();
            SetUI(AuthState.Idle, SIGNED_OUT, showError: false);
        }


        private void HandleSignInSucceeded(FirebaseUser user)
        {
#if UNITY_EDITOR
            SetUI(AuthState.SignedIn, string.Format(WELCOME_FMT, user.DisplayName ?? "Player"), showError: false);
            OnAuthSucceeded?.Invoke();
#endif
            if (_failedHandledOnce) return;

            _busy = false;
            StopAttemptTimeout();
            _attempt = AttemptKind.None;

            SetUI(AuthState.SignedIn, string.Format(WELCOME_FMT, user.DisplayName ?? "Player"), showError: false);
            OnAuthSucceeded?.Invoke();
        }

        private void HandleSignInFailed(string rawMessage)
        {
            if (_failedHandledOnce) return;
            _failedHandledOnce = true;

            _busy = false;
            StopAttemptTimeout();

            string userMsg = MapErrorToUserMessage(rawMessage);

            if (_attempt == AttemptKind.Silent)
            {
                _attempt = AttemptKind.None;

                if (_statusText) _statusText.text = AUTO_FAIL_PROMPT;
                EnableButtons(interactive: true);
                _errorPanel?.SetActive(false);

                OnAuthFailed?.Invoke(rawMessage);
                return;
            }

            _attempt = AttemptKind.None;
            SetUI(AuthState.Failed, userMsg, showError: true);
            OnAuthFailed?.Invoke(rawMessage);
        }

        private void OnClickRetry()
        {
            _errorPanel?.SetActive(false);
            OnClickGoogleLogin();
        }


        private bool BeginBusy(string message, AttemptKind attempt)
        {
            if (_busy) return false;
            _busy = true;
            _attempt = attempt;
            SetUI(AuthState.Busy, message, showError: false);
            StartAttemptTimeoutIfNeeded();
            return true;
        }

        private void StartAttemptTimeoutIfNeeded()
        {
            StopAttemptTimeout();
            if (!isActiveAndEnabled) return;
            if (_attempt == AttemptKind.Silent && _silentTimeoutSec > 0f)
                _timeoutCO = StartCoroutine(SilentTimeoutCO());
        }

        private IEnumerator SilentTimeoutCO()
        {
            yield return new WaitForSeconds(_silentTimeoutSec);
            if (_busy && _attempt == AttemptKind.Silent)
            {
                Debug.LogWarning("[AuthFlow] Silent sign-in timeout.");
                HandleSignInFailed("Silent sign-in timeout");
            }
        }

        private void StopAttemptTimeout()
        {
            if (_timeoutCO != null)
            {
                StopCoroutine(_timeoutCO);
                _timeoutCO = null;
            }
        }

        private void SetUI(AuthState state, string message, bool showError)
        {
            _state = state;
            OnAuthStateChanged?.Invoke(_state);

            if (_statusText) _statusText.text = message ?? string.Empty;

            bool showSpinner = state == AuthState.Busy;
            if (_loadingSpinner) _loadingSpinner.SetActive(showSpinner);

            bool signedIn = state == AuthState.SignedIn;

            EnableButtons(interactive: !showSpinner && !signedIn);
            if (_signOutButton) _signOutButton.gameObject.SetActive(signedIn);

            if (_errorPanel) _errorPanel.SetActive(showError);
            if (_errorText && showError) _errorText.text = message ?? LOGIN_FAIL;
        }

        private void EnableButtons(bool interactive)
        {
            if (_googleLoginButton) _googleLoginButton.interactable = interactive;
            if (_retryButton) _retryButton.interactable = interactive;
        }

        private string MapErrorToUserMessage(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return LOGIN_FAIL_NET;

            string lower = raw.ToLowerInvariant();
            if (lower.Contains("network") || lower.Contains("unreachable") || lower.Contains("timeout"))
                return "Network is unstable. Please try again with a stable connection.";
            if (lower.Contains("canceled") || lower.Contains("cancelled"))
                return "Login was canceled. Please try again.";
            if (lower.Contains("idtoken") || lower.Contains("empty"))
                return "Login configuration error occurred. Please contact the administrator.";
            if (lower.Contains("developer_error") || lower.Contains("invalid"))
                return "Login configuration is invalid. Please try again later.";

            return LOGIN_FAIL_LATER;
        }
    }
}
