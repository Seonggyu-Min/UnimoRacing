using Firebase.Auth;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace MSG
{
    public class AccountDeletionButton : MonoBehaviour
    {
        [SerializeField] private Button _deleteButton;

        public async void OnClickDeleteButton()
        {
            if (_deleteButton != null)
            {
                _deleteButton.interactable = false;
            }

            try
            {
                bool ok = await AccountDeletion.DeleteAccountAsync();
                if (ok)
                {
                    PhotonNetwork.Disconnect();

                    // 로그인 씬으로 이동
                    SceneManager.LoadScene(0);
                }
                else
                {
                    Debug.LogWarning("[AccountDeletionButton] 탈퇴 실패 또는 취소");
                }
            }
            finally
            {
                if (_deleteButton != null)
                {
                    _deleteButton.interactable = true;
                }
            }
        }
    }
}
