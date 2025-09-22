using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class MissionTapButtonBehaviour : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private float slideDistance = 800f;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Ease ease = Ease.OutCirc;

        [SerializeField] private GameObject _leftEnabledObj;
        [SerializeField] private GameObject _leftDisabledObj;
        [SerializeField] private GameObject _rightEnabledObj;
        [SerializeField] private GameObject _rightDisabledObj;

        private Vector2 _originPos;
        private bool _isLeft = true;


        private void Awake()
        {
            _originPos = panel.anchoredPosition;
        }

        private void OnEnable()
        {
            ResetPosition();
            _leftEnabledObj.SetActive(true);
            _leftDisabledObj.SetActive(false);
            _rightEnabledObj.SetActive(false);
            _rightDisabledObj.SetActive(true);
        }


        public void ShowLeftPanel()
        {
            if (_isLeft) return;

            DOTween.Kill(panel);
            panel.DOAnchorPos(_originPos, duration)
                 .SetEase(ease)
                 .SetUpdate(true)
                 .OnComplete(() =>
                 {
                     _isLeft = true;
                     _leftEnabledObj.SetActive(true);
                     _leftDisabledObj.SetActive(false);
                     _rightEnabledObj.SetActive(false);
                     _rightDisabledObj.SetActive(true);
                 });
        }

        public void ShowRightPanel()
        {
            if (!_isLeft) return;

            DOTween.Kill(panel);
            panel.DOAnchorPos(_originPos + Vector2.left * slideDistance, duration)
                 .SetEase(ease)
                 .SetUpdate(true)
                 .OnComplete(() =>
                 { 
                     _isLeft = false;
                     _leftEnabledObj.SetActive(false);
                     _leftDisabledObj.SetActive(true);
                     _rightEnabledObj.SetActive(true);
                     _rightDisabledObj.SetActive(false);
                 });
        }

        public void ResetPosition()
        {
            DOTween.Kill(panel);
            panel.DOAnchorPos(_originPos, duration)
                 .SetEase(ease)
                 .SetUpdate(true);

            _isLeft = true;
        }
    }
}
