using EditorAttributes;
using Firebase.Database;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace MSG
{
    public class ConnectionFailureWatcher : MonoBehaviourPunCallbacks
    {
        [Header("Navigation")]
        [SerializeField] private int _mainScene = 0; // 메인 씬 인덱스
        [SerializeField] private float _forceLoadDelaySec = 0.5f; // 정리 딜레이 후 강제 로드 시간

        [Header("Options")]
        [SerializeField] private bool _disconnectPhotonOnExit = true; // 돌아갈 때 Photon Disconnect 시도
        [SerializeField] private float _initialGraceSec = 5f; // 초기 false 이벤트 무시용

        private bool _returning;
        private bool _armed;               // 초기 구간 지나면 true
        private bool _fbEverConnected;     // Firebase가 true 된 적 있는지
        private bool _punEverConnected;    // PUN이 마스터에 연결된 적 있는지

        private DatabaseReference _fbConnectedRef;
        private ConcurrentQueue<Action> _mainThreadQueue = new();


        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 초기 그레이스 기간 후 armed
            StartCoroutine(ArmAfterDelayRoutine(_initialGraceSec));
        }

        private IEnumerator ArmAfterDelayRoutine(float sec)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, sec));
            _armed = true;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            try
            {
                _fbConnectedRef = FirebaseManager.Instance.Database.GetReference(".info/connected");
                _fbConnectedRef.ValueChanged += OnFirebaseConnectedChanged;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConnectionWatcher] Firebase hook 실패: {e.Message}");
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();

            if (_fbConnectedRef != null)
            {
                try 
                { 
                    _fbConnectedRef.ValueChanged -= OnFirebaseConnectedChanged; 
                }
                catch { }
                _fbConnectedRef = null;
            }
        }

        private void Update()
        {
            // Firebase 스레드 -> 메인스레드 디스패치 큐 처리
            while (_mainThreadQueue.TryDequeue(out var action))
            {
                try { action?.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }


        public override void OnConnectedToMaster()
        {
            _punEverConnected = true;
        }

        // PUN2: 네트워크 끊김
        public override void OnDisconnected(DisconnectCause cause)
        {
            // 앱 시작 직후 등, 한 번도 연결된 적이 없으면 무시
            if (!_punEverConnected || !_armed)
            {
                Debug.Log($"[ConnectionWatcher] Ignore OnDisconnected (ever:{_punEverConnected}, armed:{_armed}) cause={cause}");
                return;
            }

            Debug.LogWarning($"[ConnectionWatcher] PUN Disconnected: {cause}");
            TriggerReturnToMain($"PUN Disconnected: {cause}");
        }


        // Firebase: .info/connected 변경
        private void OnFirebaseConnectedChanged(object sender, ValueChangedEventArgs e)
        {
            if (e.DatabaseError != null) return;

            bool isConnected = false;
            try
            {
                if (e.Snapshot != null && e.Snapshot.Value is bool b) isConnected = b;
            }
            catch { }

            if (isConnected)
            {
                _fbEverConnected = true; // 한 번이라도 true를 받았음
                return;
            }

            // false가 왔어도, 아직 armed 전이거나 true를 본 적이 없으면 초기 false이므로 무시
            if (!_armed || !_fbEverConnected)
            {
                Debug.Log($"[ConnectionWatcher] Ignore Firebase false (armed:{_armed}, ever:{_fbEverConnected})");
                return;
            }

            EnqueueOnMainThread(() =>
            {
                Debug.LogWarning("[ConnectionWatcher] Firebase disconnected");
                TriggerReturnToMain("Firebase disconnected");
            });
        }

        // 공통 처리
        private void TriggerReturnToMain(string reason)
        {
            if (_returning) return;
            _returning = true;

            Debug.LogWarning($"[ConnectionWatcher] Return to main: {reason}");

            // 정리 시도
            if (_disconnectPhotonOnExit)
            {
                try
                {
                    if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom(false);
                    if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ConnectionWatcher] Photon 정리 중 예외: {e.Message}");
                }
            }

            // 약간의 딜레이 후 강제 씬 로드
            StartCoroutine(ForceLoadMainAfterDelayRoutine(_forceLoadDelaySec));
        }

        private IEnumerator ForceLoadMainAfterDelayRoutine(float delay)
        {
            float until = Time.unscaledTime + Mathf.Max(0f, delay);
            while (Time.unscaledTime < until) yield return null;

            try
            {
                SceneManager.LoadScene(_mainScene);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConnectionWatcher] 씬 로드 실패: {_mainScene}번째 씬, {e.Message}");
            }
        }

        private void EnqueueOnMainThread(Action action)
        {
            if (action != null) _mainThreadQueue.Enqueue(action);
        }


        [Button("Test Return To Main")]
        private void TestReturn() => TriggerReturnToMain("Manual Test");
    }
}

