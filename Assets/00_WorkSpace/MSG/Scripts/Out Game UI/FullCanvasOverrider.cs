using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace MSG
{
    /// <summary>
    /// Safe Area Fitter를 무시하고 해당 컴포넌트가 붙은 Image로 하여금 Canvas 전체를 덮도록 합니다
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FullCanvasOverrider : UIBehaviour
    {
        [SerializeField] private Image image;                 // 배경 이미지
        [Range(0, 1)] public float alignX = 0.5f;             // 넘친 부분 정렬
        [Range(0, 1)] public float alignY = 0.5f;
        [SerializeField] private bool ignoreMasks = true;     // 마스크/RectMask2D 무시
        [SerializeField] private bool ignoreLayout = true;    // 레이아웃 간섭 무시

        private RectTransform rt;
        private RectTransform rootCanvasRT;

        protected override void Awake()
        {
            rt = (RectTransform)transform;
            if (image == null) image = GetComponent<Image>();

            var rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            rootCanvasRT = rootCanvas ? (RectTransform)rootCanvas.transform : null;

            if (ignoreMasks && image != null) image.maskable = false;
            if (ignoreLayout)
            {
                var le = GetComponent<LayoutElement>();
                if (le == null) le = gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = true;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Canvas.ForceUpdateCanvases();
            Apply();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Apply();
        }

        private void Apply()
        {
            if (rootCanvasRT == null || image == null) return;

            // 루트 캔버스 크기
            Rect target = rootCanvasRT.rect;
            float targetW = target.width;
            float targetH = target.height;

            // 이미지 원본 비율
            float imgW, imgH;
            var spr = image.sprite;
            var tex = image.mainTexture;
            if (spr != null) { imgW = spr.rect.width; imgH = spr.rect.height; }
            else if (tex != null) { imgW = tex.width; imgH = tex.height; }
            else return;

            float imgAspect = imgW / imgH;
            float targetAspect = targetW / targetH;

            // cover 사이즈
            float w, h;
            if (imgAspect < targetAspect) { w = targetW; h = w / imgAspect; }
            else { h = targetH; w = h * imgAspect; }

            // 부모의 중심과 Canvas 중심의 월드 좌표
            RectTransform parentRT = (RectTransform)rt.parent;
            Vector3 parentCenterWS = parentRT.TransformPoint(parentRT.rect.center);
            Vector3 canvasCenterWS = rootCanvasRT.TransformPoint(rootCanvasRT.rect.center);

            // 월드 오프셋을 부모 로컬 좌표로 변환
            Vector3 canvasCenterInParentLocal = parentRT.InverseTransformPoint(canvasCenterWS);
            Vector3 parentCenterInParentLocal = parentRT.InverseTransformPoint(parentCenterWS); // 보통 (0,0)
            Vector2 centerOffsetLocal = (Vector2)(canvasCenterInParentLocal - parentCenterInParentLocal);

            // 배치
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);

            // 넘친 면 정렬 오프셋
            Vector2 alignOffset = new Vector2(
                (w - targetW) * (alignX - 0.5f),
                (h - targetH) * (alignY - 0.5f)
            );

            // 최종 위치 = Canvas 중심 + 정렬 오프셋
            rt.anchoredPosition = centerOffsetLocal + alignOffset;
        }
    }
}
