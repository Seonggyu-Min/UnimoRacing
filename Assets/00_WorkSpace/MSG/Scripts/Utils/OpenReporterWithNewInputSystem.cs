using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;


namespace MSG
{
    public class OpenReporterWithNewInputSystem : MonoBehaviour
    {
        [SerializeField] private float holdSec = 0.6f; // 3손가락 길게누르기
        [SerializeField] private Reporter reporter;
        private float pressStart = -1f;

        void Awake()
        {
            if (reporter == null)
            {
                reporter = FindObjectOfType<Reporter>();
                if (reporter == null)
                {
                    Debug.LogError("Reporter not found in the scene. Please add a Reporter component.");
                    return;
                }
            }

            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            TouchSimulation.Enable();
#endif
        }

        void Update()
        {
            var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            if (touches.Count >= 3)
            {
                if (pressStart < 0f) pressStart = Time.unscaledTime;
                else if (Time.unscaledTime - pressStart >= holdSec)
                {
                    if (reporter) reporter.Toggle();
                    pressStart = -1f;
                }
            }
            else pressStart = -1f;
        }
    }
}
