using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public enum DeveloperAccountType
    {
        None,
        MSG1, MSG2, MSG3, MSG4,
        YSJ1, YSJ2, YSJ3, YSJ4,
        PJY1, PJY2, PJY3, PJY4,
        YTW1, YTW2, YTW3, YTW4,
        NTJ1, NTJ2, NTJ3, NTJ4
    }

    public class AutoLogIn : MonoBehaviour
    {
        [Header("UIs")]
        [SerializeField] private TMP_Dropdown _accountDropdown;
        [SerializeField] private TMP_InputField _emailInputField;
        [SerializeField] private TMP_InputField _passwordInputField;
        [SerializeField] private DevModeLogInHelper _devModeLogInHelper;

        [Header("AccountSO")]
        [Header("MSG")]
        [SerializeField] private AccountSO _msgAccount;
        [Header("YSJ")]
        [SerializeField] private AccountSO _ysjAccount;
        [Header("PJY")]
        [SerializeField] private AccountSO _pjyAccount;
        [Header("YTW")]
        [SerializeField] private AccountSO _ytwAccount;
        [Header("NTJ")]
        [SerializeField] private AccountSO _ntjAccount;

        private Dictionary<DeveloperAccountType, (string email, string password)> _developerCredentials;

        private void Start()
        {
            _developerCredentials = new()
            {
                { DeveloperAccountType.MSG1, (_msgAccount.Email1, _msgAccount.Password1) },
                { DeveloperAccountType.MSG2, (_msgAccount.Email2, _msgAccount.Password2) },
                { DeveloperAccountType.MSG3, (_msgAccount.Email3, _msgAccount.Password3) },
                { DeveloperAccountType.MSG4, (_msgAccount.Email4, _msgAccount.Password4) },

                { DeveloperAccountType.YSJ1, (_ysjAccount.Email1, _ysjAccount.Password1) },
                { DeveloperAccountType.YSJ2, (_ysjAccount.Email2, _ysjAccount.Password2) },
                { DeveloperAccountType.YSJ3, (_ysjAccount.Email3, _ysjAccount.Password3) },
                { DeveloperAccountType.YSJ4, (_ysjAccount.Email4, _ysjAccount.Password4) },

                { DeveloperAccountType.PJY1, (_pjyAccount.Email1, _pjyAccount.Password1) },
                { DeveloperAccountType.PJY2, (_pjyAccount.Email2, _pjyAccount.Password2) },
                { DeveloperAccountType.PJY3, (_pjyAccount.Email3, _pjyAccount.Password3) },
                { DeveloperAccountType.PJY4, (_pjyAccount.Email4, _pjyAccount.Password4) },

                { DeveloperAccountType.YTW1, (_ytwAccount.Email1, _ytwAccount.Password1) },
                { DeveloperAccountType.YTW2, (_ytwAccount.Email2, _ytwAccount.Password2) },
                { DeveloperAccountType.YTW3, (_ytwAccount.Email3, _ytwAccount.Password3) },
                { DeveloperAccountType.YTW4, (_ytwAccount.Email4, _ytwAccount.Password4) },

                { DeveloperAccountType.NTJ1, (_ntjAccount.Email1, _ntjAccount.Password1) },
                { DeveloperAccountType.NTJ2, (_ntjAccount.Email2, _ntjAccount.Password2) },
                { DeveloperAccountType.NTJ3, (_ntjAccount.Email3, _ntjAccount.Password3) },
                { DeveloperAccountType.NTJ4, (_ntjAccount.Email4, _ntjAccount.Password4) },
            };

            _accountDropdown.ClearOptions();

            List<string> options = new();
            foreach (DeveloperAccountType type in Enum.GetValues(typeof(DeveloperAccountType)))
            {
                options.Add(type.ToString());
            }

            _accountDropdown.AddOptions(options);
            _accountDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        private void OnDropdownValueChanged(int index)
        {
            DeveloperAccountType selected = (DeveloperAccountType)index;

            if (_developerCredentials.TryGetValue(selected, out var credentials))
            {
                _emailInputField.text = credentials.email;
                _passwordInputField.text = credentials.password;
            }
            else
            {
                _emailInputField.text = string.Empty;
                _passwordInputField.text = string.Empty;
            }

            if (selected != DeveloperAccountType.None)
            {
                _devModeLogInHelper.OnClickLogInOrCreateButton();
            }
        }
    }
}
