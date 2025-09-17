using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MSG
{
    [RequireComponent(typeof(UIAnimator))]
    [DisallowMultipleComponent]
    public class UIUnit : MonoBehaviour
    {
        [SerializeField] private UIAnimator _animator;

        private void Awake()
        {
            if (_animator == null)
            {
                if (!TryGetComponent(out _animator))
                {
                    Debug.LogWarning("UIAnimator를 찾을 수 없습니다.");
                }
            }
        }

        private void OnEnable() => ShowAnimation();


        public void ShowAnimation()
        {
            if (_animator != null)
            {
                StartCoroutine(_animator.EnableAnimation());
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        public void HideAnimation(Action onComplete = null)
        {
            if (_animator != null)
            {
                StartCoroutine(_animator.DisableAnimation(() =>
                {
                    gameObject.SetActive(false);
                    onComplete?.Invoke();
                }));
            }
            else // 애니메이션이 없는 경우 바로 비활성화
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            }
        }
    }
}
