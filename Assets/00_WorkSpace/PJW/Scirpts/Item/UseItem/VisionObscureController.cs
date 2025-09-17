using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PJW
{
    /// <summary>
    /// 화면을 간단히 가렸다가 풀어주는 컨트롤러
    /// </summary>
    public class VisionObscureController : MonoBehaviour
    {
        public static VisionObscureController Instance { get; private set; }

        private Image overlay;
        private Coroutine running;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureOverlay();
        }

        private void EnsureOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var go = new GameObject("Overlay");
            go.transform.SetParent(transform, false);
            overlay = go.AddComponent<Image>();
            overlay.raycastTarget = false;
            overlay.color = new Color(0f, 0f, 0f, 0f);

            var rt = overlay.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            go.SetActive(false);
        }

        public void Obscure(float hold, float fadeIn, float maxAlpha, float fadeOut)
        {
            if (overlay == null) EnsureOverlay();
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(CoObscure(hold, Mathf.Max(0f, fadeIn), Mathf.Clamp01(maxAlpha), Mathf.Max(0f, fadeOut)));
        }

        private IEnumerator CoObscure(float hold, float fi, float maxA, float fo)
        {
            overlay.gameObject.SetActive(true);

            // Fade In
            float t = 0f;
            while (t < fi)
            {
                t += Time.unscaledDeltaTime;
                float a = (fi > 0f) ? Mathf.Lerp(0f, maxA, t / fi) : maxA;
                overlay.color = new Color(0f, 0f, 0f, a);
                yield return null;
            }
            overlay.color = new Color(0f, 0f, 0f, maxA);

            // Hold
            t = 0f;
            while (t < hold) { t += Time.unscaledDeltaTime; yield return null; }

            // Fade Out
            t = 0f;
            while (t < fo)
            {
                t += Time.unscaledDeltaTime;
                float a = (fo > 0f) ? Mathf.Lerp(maxA, 0f, t / fo) : 0f;
                overlay.color = new Color(0f, 0f, 0f, a);
                yield return null;
            }
            overlay.color = new Color(0f, 0f, 0f, 0f);
            overlay.gameObject.SetActive(false);

            running = null;
        }

        public static VisionObscureController EnsureInScene()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("VisionObscureController");
            return go.AddComponent<VisionObscureController>();
        }
    }
}
