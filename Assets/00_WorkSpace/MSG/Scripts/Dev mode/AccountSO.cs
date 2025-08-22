using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    [CreateAssetMenu(fileName = "AccountSO", menuName = "ScriptableObjects/AccountSO")]
    public class AccountSO : ScriptableObject
    {
        [Header("Account Infos, Should Be Email Login NOT GOOGLE")]
        [Header("1st Account")]
        public string Email1;
        public string Password1;
        [Header("2nd Account")]
        public string Email2;
        public string Password2;
        [Header("3rd Account")]
        public string Email3;
        public string Password3;
        [Header("4th Account")]
        public string Email4;
        public string Password4;
    }
}
