using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _lastSafe;
        private Vector2 _lastSize;

        void Awake() => _rt = (RectTransform)transform;

        void OnEnable() => Apply();

        void Apply()
        {
            var sa = Screen.safeArea;
            var min = sa.position;
            var max = sa.position + sa.size;

            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;

            _lastSafe = sa;
            _lastSize = new Vector2(Screen.width, Screen.height);
        }
    }
}
