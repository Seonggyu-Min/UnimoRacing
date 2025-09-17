using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform RT => (RectTransform)transform;
        private Rect _lastSafe;
        private Vector2Int _lastRes;

        //void Awake() => Apply();

        void Start() => Apply();

        void OnRectTransformDimensionsChange() => Apply();

        void Apply()
        {
            Rect safe = Screen.safeArea;
            if (safe == _lastSafe && _lastRes.x == Screen.width && _lastRes.y == Screen.height)
                return;

            _lastSafe = safe;
            _lastRes = new Vector2Int(Screen.width, Screen.height);

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            RT.anchorMin = min;
            RT.anchorMax = max;
            RT.offsetMin = Vector2.zero;
            RT.offsetMax = Vector2.zero;
        }
    }
}
