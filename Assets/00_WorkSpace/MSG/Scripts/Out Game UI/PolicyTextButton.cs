using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class PolicyTextButton : MonoBehaviour
    {
        [SerializeField] private string _privacyURL;
        [SerializeField] private string _refundURL;

        public void OnClickPrivacyURL()
        {
            Application.OpenURL(_privacyURL);
        }

        public void OnClickRefundURL()
        {
            Application.OpenURL(_refundURL);
        }
    }
}