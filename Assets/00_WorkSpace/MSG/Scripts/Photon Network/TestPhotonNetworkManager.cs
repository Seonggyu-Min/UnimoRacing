using Firebase.Auth;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace MSG
{
    public class TestPhotonNetworkManager : MonoBehaviour
    {
        [SerializeField] private AuthFlowController _authFlowController;
        [SerializeField] private Button _nextSceneButton;

        private bool _isLoggedIn = false;
        private bool _isFirebaseReady = false;


        private void Start()
        {
            _authFlowController.OnAuthSucceeded += SetLoggedInReady;

            if (FirebaseManager.Instance.IsReady)
            {
                SetFirebaseReady();
            }
            else
            {
                FirebaseManager.Instance.OnFirebaseReady += SetFirebaseReady;
            }
        }

        private void OnDestroy()
        {
            if (_authFlowController != null)
            {
                _authFlowController.OnAuthSucceeded -= SetLoggedInReady;
            }
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.OnFirebaseReady -= SetFirebaseReady;
            }
        }


        private void SetFirebaseReady()
        {
            _isFirebaseReady = true;
            SetUid();
        }

        private void SetLoggedInReady()
        {
            _isLoggedIn = true;
            SetUid();
        }


        private void SetUid()
        {
            if (!_isLoggedIn || !_isFirebaseReady)
            {
                Debug.Log("로그인 또는 Firebase 준비가 완료되지 않았습니다.");
                return;
            }

            PhotonNetwork.AuthValues = 
                new Photon.Realtime.AuthenticationValues(FirebaseManager.Instance.Auth.CurrentUser.UserId);
            PhotonNetwork.NickName = FirebaseManager.Instance.Auth.CurrentUser.DisplayName;

            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.AutomaticallySyncScene = true;

            Debug.Log($"[TestPhotonNetworkManager] Firebase와 Photon 연결 완료. 다음 씬으로 이동 가능합니다");
            Debug.Log($"파이어베이스 uid : {FirebaseManager.Instance.Auth.CurrentUser.UserId}");
            Debug.Log($"포톤uid : {PhotonNetwork.AuthValues}");

            _nextSceneButton.gameObject.SetActive(true);
        }

        public void OnClickNextSceneButton()
        {
            SceneManager.LoadScene(1); // 임시
        }
    }
}
