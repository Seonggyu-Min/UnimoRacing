using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class FrameDebugger : MonoBehaviour
    {
        private float deltaTime = 0f;

        [Header("폰트 옵션")]
        [SerializeField] private int size = 25;
        [SerializeField][Min(0f)] private float xPos = 30f;
        [SerializeField][Min(0f)] private float yPos = 30f;
        [SerializeField] private Color color = Color.red;

        [Header("평균 FPS 옵션")]
        [SerializeField] private bool _enableAverage = false;
        [SerializeField, Min(1)] private int _averageSampleCount = 1000; // 최근 120프레임 = 2초 정도 (60fps 기준)

        private Queue<float> _fpsSamples = new();
        private float _fpsSum = 0f;

        void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

            if (_enableAverage)
            {
                float fps = 1.0f / deltaTime;

                // 새 값 추가
                _fpsSamples.Enqueue(fps);
                _fpsSum += fps;

                // 오래된 값 제거
                if (_fpsSamples.Count > _averageSampleCount)
                {
                    _fpsSum -= _fpsSamples.Dequeue();
                }
            }
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = size;
            style.normal.textColor = color;

            float ms = deltaTime * 1000f;
            float fps = 1.0f / deltaTime;
            string text = string.Format("{0:0.} FPS ({1:0.0} ms)", fps, ms);

            if (_enableAverage && _fpsSamples.Count > 0)
            {
                float avgFps = _fpsSum / _fpsSamples.Count;
                text += string.Format("\nAvg: {0:0.} FPS", avgFps);
            }

            GUI.Label(new Rect(xPos, yPos, Screen.width, Screen.height), text, style);
        }
    }
}
