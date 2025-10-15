using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MuteButtonBehaviour : MonoBehaviour
{
    [SerializeField] private Image _soundIcon;
    [SerializeField] private Slider _silder;

    [SerializeField] private Sprite _muteSprite;
    [SerializeField] private Sprite _unmuteSprite;

    private float _originValue = 0.5f;


    private void OnEnable()
    {
        if (_silder != null)
        {
            _soundIcon.sprite = _silder.value == 0 ? _muteSprite : _unmuteSprite;
        }
        _silder.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDisable()
    {
        if (_silder != null)
        {
            _silder.onValueChanged.RemoveListener(OnValueChanged);
        }
    }
    private void OnValueChanged(float value)
    {
        if (_silder != null)
        {
            _soundIcon.sprite = value == 0 ? _muteSprite : _unmuteSprite;
        }
    }


    public void OnClickIcon()
    {
        if (_silder != null)
        {
            if (_silder.value == 0) // 음소거 일 때
            {
                _silder.value = _originValue;
                _soundIcon.sprite = _unmuteSprite;
            }
            else // 음소거가 아닐 때
            {
                _originValue = _silder.value;
                _silder.value = 0f;
                _soundIcon.sprite = _muteSprite;
            }
        }
    }
}