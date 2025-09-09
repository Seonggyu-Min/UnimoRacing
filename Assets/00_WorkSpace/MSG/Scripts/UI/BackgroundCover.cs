using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    [RequireComponent(typeof(RectTransform))]
    public class BackgroundCover : MonoBehaviour
    {
        [SerializeField] private Image image;
        [Range(0, 1)] public float alignX = 0.5f;
        [Range(0, 1)] public float alignY = 0.5f;

        RectTransform rt;
        RectTransform parentRT;

        void Awake()
        {
            rt = (RectTransform)transform;
            if (image == null) image = GetComponent<Image>();
            parentRT = rt.parent as RectTransform;
        }

        void OnEnable()
        {
            StartCoroutine(ApplyNextFrame());
        }

        IEnumerator ApplyNextFrame()
        {
            yield return null; // 한 프레임 대기
            Apply();
        }

        void Apply()
        {
            if (image == null || parentRT == null) return;
            var spr = image.sprite;
            var tex = image.mainTexture;

            // 이미지 비율 계산
            float imgW, imgH;
            if (spr != null)
            {
                imgW = spr.rect.width;
                imgH = spr.rect.height;
            }
            else if (tex != null)
            {
                imgW = tex.width;
                imgH = tex.height;
            }
            else return;

            var target = parentRT.rect;
            float targetW = target.width;
            float targetH = target.height;

            float imgAspect = imgW / imgH;
            float targetAspect = targetW / targetH;

            // 한 쪽을 꽉 채우고 다른 쪽은 넘치도록
            float w, h;
            if (imgAspect < targetAspect)
            {
                // 가로가 더 넓은 화면일 때, 가로를 맞추고 세로를 넘치게
                w = targetW;
                h = w / imgAspect;
            }
            else
            {
                // 세로가 더 높은 화면일 때, 세로를 맞추고 가로를 넘치게
                h = targetH;
                w = h * imgAspect;
            }

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);

            // 넘친 부분 가운데 정렬
            float offsetX = (w - targetW) * (alignX - 0.5f);
            float offsetY = (h - targetH) * (alignY - 0.5f);
            rt.anchoredPosition = new Vector2(offsetX, offsetY);
        }
    }
}
