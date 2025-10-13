using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace MSG
{
    public class InputFieldResetter : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _inputField;

        private void OnDisable()
        {
            ResetInputField();
        }

        public void ResetInputField()
        {
            _inputField.text = string.Empty;
        }
    }
}
