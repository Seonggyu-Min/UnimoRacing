using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace MSG.Deprecated
{
    #region Depricated

    /// <summary>
    /// 씬이 전환될 때 파티 및 홈룸에 복귀를 보장해주는 컴포넌트입니다.
    /// 더 이상 사용되지 않습니다.
    /// </summary>
    public class RoomGuarantor : MonoBehaviour
    {
        [SerializeField] private RoomAgent _room;
        [SerializeField] private bool _willDecideByName = false;    // 로비씬을 카운트로 판단할지 이름으로 판단할지
        [SerializeField] private int _lobbySceneIndex = 1;          // 로비씬 몇 번째인지
        [SerializeField] private string _lobbySceneName;            // 로비 씬 이름

        private bool _isEnsuring; // 중복 진입 방지

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;
        private RoomAgent Room
        {
            get
            {
                if (_room == null)
                {
                    _room = FindObjectOfType<RoomAgent>();
                }
                return _room;
            }
        }


        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (PartyService.Instance != null)
            {
                PartyService.Instance.OnPartyChanged += OnPartyChanged;
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (PartyService.Instance != null)
            {
                PartyService.Instance.OnPartyChanged -= OnPartyChanged;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 로비에서만 룸 보장하기
            if (_willDecideByName)
            {
                if (scene.name == _lobbySceneName)
                {
                    _ = EnsureCorrectHomeAsync();
                }
            }
            else
            {
                if (scene.buildIndex == _lobbySceneIndex)
                {
                    _ = EnsureCorrectHomeAsync();
                }
            }
        }

        // 파티 변경 시
        private void OnPartyChanged()
        {
            if (_willDecideByName)
            {
                // 로비면 즉시 홈 보장
                if (SceneManager.GetActiveScene().name == _lobbySceneName)
                {
                    _ = EnsureCorrectHomeAsync();
                }
            }
            else
            {
                if (SceneManager.GetActiveScene().buildIndex == _lobbySceneIndex)
                {
                    _ = EnsureCorrectHomeAsync();
                }
            }
        }

        public async Task EnsureCorrectHomeAsync()
        {
            if (_isEnsuring) return;
            _isEnsuring = true;

            try
            {
                await WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer);

                string home = GetCurrentHome();

                // 이미 원하는 홈 혹은 파티방인지 확인
                if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null &&
                    PhotonNetwork.CurrentRoom.Name == home)
                    return;

                await Room.EnsureHomeRoomAsync(home);
            }
            finally
            {
                _isEnsuring = false;
            }
        }

        private static async Task WaitUntil(Func<bool> predicate)
        {
            while (!predicate())
            {
                await Task.Yield();
            }
        }

        private string GetCurrentHome()
        {
            if (PartyService.Instance.IsInParty)
            {
                return RoomMakeHelper.PartyHome(PartyService.Instance.LeaderUid);
            }
            return RoomMakeHelper.Personal(CurrentUid);
        }
    }

        #endregion
}
