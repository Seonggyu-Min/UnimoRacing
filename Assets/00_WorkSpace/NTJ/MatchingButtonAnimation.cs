using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MatchingButtonAnimation : MonoBehaviour
{
    [Header("텍스트 애니메이션 설정")]
    [SerializeField] private TMP_Text _matchingText;
    [SerializeField] private float _dotAnimIntervalTime;
    [SerializeField] private string[] _matchingTexts;

    private Coroutine _textCO;

    [Header("유니모 이미지 애니메이션 설정")]
    [SerializeField] private Transform _unimoTransform;
    [SerializeField] private float _moveDistance;
    [SerializeField] private float _moveIntervalTime;
    [SerializeField] private Ease _ease;

    private Tween _tween;
    //private void Start()
    //{
    //    gameObject.SetActive(false);
    //}

    private void OnEnable()
    {
        if (_textCO != null)
        {
            StopCoroutine(_textCO);
            _textCO = null;
        }
        _textCO = StartCoroutine(DotAnimationRoutine());

        _tween = _unimoTransform.DOMoveY(_unimoTransform.position.y + _moveDistance, _moveIntervalTime)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(_ease);
    }

    private void OnDisable()
    {
        if (_textCO != null)
        {
            StopCoroutine(_textCO);
            _textCO = null;
        }

        _matchingText.text = _matchingTexts[0];

        _tween.Kill();
    }


    private IEnumerator DotAnimationRoutine()
    {
        int i = 0;
        _matchingText.text = _matchingTexts[0];
        i++;

        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= _dotAnimIntervalTime)
            {
                _matchingText.text = _matchingTexts[i];

                i = (i + 1) % _matchingTexts.Length;
                elapsed = 0f;
            }

            yield return null;
        }
    }
}
