using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MSG
{
    public class NetworkChecker : MonoBehaviour
    {
        [SerializeField] private GameObject _infoObj;

        private void Start()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                _infoObj.gameObject.SetActive(true);
                Debug.LogWarning("네트워크 연결 안 됨");
            }
            else
            {
                _infoObj.gameObject.SetActive(false);
                Debug.Log("네트워크 연결됨");
            }
        }

        public void OnClickExitButton()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
        }
    }
}
