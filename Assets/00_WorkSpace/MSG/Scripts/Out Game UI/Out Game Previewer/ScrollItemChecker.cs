using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace MSG
{
    public class ScrollItemChecker : Singleton<ScrollItemChecker>
    {
        [Header("옵션")]
        [SerializeField] private bool _unbindWhenInvisible = true;   // 보이지 않을 때 Unbind
        [SerializeField] private float _edgePadding = 20f;           // 픽셀 기준 뷰포트 여유. 있으면 미리 로딩할 수 있음

        private RectTransform _viewport;
        private ScrollRect _scrollRect;
        private Rect _viewRect;

        private IDictionary<int, ShopUIBinder> _items;  // 실제로는 Dictionary만 쓰고 있으니까 타입을 List가 아니라 IEnumerable으로 바꿔도 될 듯

        private UnityAction<Vector2> _onScrollChanged;

        private void Awake()
        {
            SingletonInit();
        }

        public void Register(ScrollRect scrollRect, IDictionary<int, ShopUIBinder> items)
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

            UpdateViewRect();
            CheckVisibleAll();
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
    }
}
