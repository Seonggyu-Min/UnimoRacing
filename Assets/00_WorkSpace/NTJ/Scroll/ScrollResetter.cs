using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

    [DisallowMultipleComponent]
    public class ScrollResetter : MonoBehaviour
    {
        [SerializeField] private bool resetOnEnable = true;

        private void OnEnable()
        {
            if (resetOnEnable)
                StartCoroutine(ResetNextFrame());
        }

        private IEnumerator ResetNextFrame()
        {
            yield return null; // 1프레임 대기 후 실행
            Canvas.ForceUpdateCanvases();

            var scrollRects = GetComponentsInChildren<ScrollRect>(true);
            foreach (var sr in scrollRects)
            {
                sr.verticalNormalizedPosition = 1f; // 맨 위
                Debug.Log($"{name} 스크롤 초기화됨: {sr.name}");
            }
        }

        // 필요할 때 외부에서 수동 호출
        public void ResetNow()
        {
            StartCoroutine(ResetNextFrame());
        }
    }