﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using YSJ;

public class PopupBaseUI : BaseUI, IPopup
{
    #region Field
    [SerializeField] private float _showTime = 1.0f;
    [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.5f); // 반투명 검정
    [SerializeField] private GameObject _popupBody;

    [SerializeField] private bool _isDestructible = true;

    private GameObject _background;

    public float ShowTime => _showTime;


    #endregion

    #region Unity Method
    private void Start() => Open();

    #endregion

    #region BaseUI Method
    public virtual void Open()
    {
        CreateBackground();
        StartCoroutine(ShowAnimation());
    }
    public virtual void Close()
    {
        if (!_isDestructible) return;

        if (_background != null)
            Destroy(_background);
        Destroy(this.gameObject);
    }

    #endregion

    #region Popup Method

    public void OnClickClose() => Close();

    private IEnumerator ShowAnimation()
    {
        if (_popupBody == null) yield break;

        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;

        _popupBody.transform.localScale = startScale;

        while (elapsed < _showTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _showTime);
            _popupBody.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        _popupBody.transform.localScale = endScale;
    }

    private void CreateBackground()
    {
        // 텍스처 생성
        var bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, _backgroundColor);
        bgTex.Apply();

        // 배경 오브젝트 생성
        _background = new GameObject("PopupBackground");
        var image = _background.AddComponent<Image>();
        var sprite = Sprite.Create(bgTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        image.sprite = sprite;

        // 머티리얼 분리
        image.material = new Material(image.material);
        image.material.mainTexture = bgTex;

        // 색상 적용 + 알파 페이드
        image.color = _backgroundColor;
        image.canvasRenderer.SetAlpha(0f);
        image.CrossFadeAlpha(1f, 0.4f, false);

        _background.transform.SetParent(this.transform, false);
        _background.transform.localScale = Vector3.one;

        // 화면 전체 크기로 설정
        var rectTransform = _background.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // 팝업 바로 뒤로 배치
        _background.transform.SetAsFirstSibling();
    }

    #endregion
}
