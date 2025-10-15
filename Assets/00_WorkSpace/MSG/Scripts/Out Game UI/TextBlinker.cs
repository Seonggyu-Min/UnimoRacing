using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace MSG
{
    public class TextBlinker : MonoBehaviour
    {
        [SerializeField] private TMP_Text _blinkText;
        [SerializeField] private float _blinkInterval;

        private string _originText;
        private float _elapsed = 0f;


        private void OnEnable()
        {
            _originText = _blinkText.text;
        }

        private void OnDisable()
        {
            _blinkText.text = _originText;
        }


        private void Update()
        {
            if (_blinkText != null)
            {
                _elapsed += Time.deltaTime;

                if (_elapsed > _blinkInterval)
                {
                    if (_blinkText.text == string.Empty)
                    {
                        _blinkText.text = _originText;
                    }
                    else
                    {
                        _blinkText.text = string.Empty;
                    }
                    _elapsed -= _blinkInterval;
                }
            }
        }
    }
}
