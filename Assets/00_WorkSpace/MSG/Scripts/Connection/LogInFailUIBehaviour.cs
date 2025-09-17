using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace MSG
{
    public class LogInFailUIBehaviour : MonoBehaviour
    {
        [SerializeField] private GameObject _errorObj;
        [SerializeField] private TMP_Text _errorText;


        private void Awake()
        {
            if (_errorObj) _errorObj.SetActive(false);
        }

        private void OnEnable()
        {
            if (GoogleSignManager.Instance != null)
                GoogleSignManager.Instance.OnSignInFailed += HandleFailed;
        }

        private void OnDisable()
        {
            if (GoogleSignManager.Instance != null)
                GoogleSignManager.Instance.OnSignInFailed -= HandleFailed;
        }

        private void HandleFailed(string msg)
        {
            if (_errorText) _errorText.text = msg;
            if (_errorObj) _errorObj.SetActive(true);
        }

        public void OnClickClose()
        {
            if (_errorObj) _errorObj.SetActive(false);
        }
    }
}
