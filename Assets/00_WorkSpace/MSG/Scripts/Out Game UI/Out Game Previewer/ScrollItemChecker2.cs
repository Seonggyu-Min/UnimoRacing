using MSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace MSG
{
    // 실제 BuyButtonBehaviour가 들어간 ScrollItemChecker입니다. 기존 ScrollItemChecker를 대체합니다.
    public class ScrollItemChecker2 : MonoBehaviour
    {
        [Header("옵션")]
        [SerializeField] private bool _unbindWhenInvisible = true;   // 보이지 않을 때 Unbind
        [SerializeField] private float _edgePadding = 20f;           // 픽셀 기준 뷰포트 여유. 있으면 미리 로딩할 수 있음

        private RectTransform _viewport;
        private ScrollRect _scrollRect;
        private Rect _viewRect;
        private Coroutine _waitCO;

        private IDictionary<int, IPreviewItem> _items;

        private UnityAction<Vector2> _onScrollChanged;

        private void OnEnable()
        {
            _waitCO = StartCoroutine(WaitAndCheck());
        }

        private void OnDestroy()
        {
            Unregister();
        }

        public void Register(ScrollRect scrollRect, IDictionary<int, IPreviewItem> items)
        {
            Unregister();

            _scrollRect = scrollRect;
            _items = items;

            if (_scrollRect == null)
            {
                Debug.LogWarning("[ScrollItemChecker] ScrollRect is null.");
                return;
            }

            _viewport = _scrollRect.viewport;
            if (_viewport == null)
            {
                Debug.LogWarning("[ScrollItemChecker] ScrollRect.viewport is null.");
                return;
            }

            _onScrollChanged = _ => CheckVisibleAll();
            _scrollRect.onValueChanged.AddListener(_onScrollChanged);

            if (isActiveAndEnabled)
            {
                _waitCO = StartCoroutine(WaitAndCheck());
            }
        }

        public void Unregister()
        {
            if (_scrollRect != null && _onScrollChanged != null)
                _scrollRect.onValueChanged.RemoveListener(_onScrollChanged);

            _onScrollChanged = null;
            _scrollRect = null;
            _viewport = null;
            _items = null;
        }

        public void CheckVisibleAll()
        {
            if (_items == null || _items.Count == 0 || _viewport == null) return;

            UpdateViewRect();
            foreach (var item in _items)
            {
                if (item.Value == null || !item.Value.isActiveAndEnabled) continue;

                var rt = (RectTransform)item.Value.transform;
                if (rt == null) continue;

                bool visible = _viewRect.Overlaps(GetWorldRect(rt), true);

                if (visible) item.Value.TryBind();
                else if (_unbindWhenInvisible) item.Value.TryUnbind();
            }
        }


        private void UpdateViewRect()
        {
            _viewRect = GetWorldRect(_viewport);

            //if (_edgePadding > 0f)
            //{
            _viewRect.xMin -= _edgePadding;
            _viewRect.yMin -= _edgePadding;
            _viewRect.xMax += _edgePadding;
            _viewRect.yMax += _edgePadding;
            //}
        }

        private Rect GetWorldRect(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            Vector2 min = new Vector2(c[0].x, c[0].y);
            Vector2 size = new Vector2(c[2].x - c[0].x, c[2].y - c[0].y);
            return new Rect(min, size);
        }

        private IEnumerator WaitAndCheck()
        {
            yield return null;
            CheckVisibleAll();
            UpdateViewRect();
            _waitCO = null;
        }
    }
}
