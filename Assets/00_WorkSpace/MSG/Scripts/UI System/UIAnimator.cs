using DG.Tweening;
using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace MSG
{
    #region Serializables

    public enum SlideDirection
    {
        Horizontal,
        Vertical
    }

    [Serializable]
    public class RectWithCanvasGroup
    {
        public RectTransform RectTransform;
        public CanvasGroup CanvasGroup;
    }

    [Serializable]
    public class UIGroup
    {
        [Header("애니메이션 타입 설정")]
        public bool UseSlide = true;
        public bool UseFade = false;

        [Header("애니메이션 대상")]
        public List<RectWithCanvasGroup> Targets;

        [Header("슬라이드 설정")]
        [ShowField(nameof(UseSlide))]
        public SlideDirection SlideDirection = SlideDirection.Horizontal;
        [ShowField(nameof(UseSlide))]
        public float OriginOffset = 0f;
        [ShowField(nameof(UseSlide))]
        public float MoveInOffset = -300f;
        [ShowField(nameof(UseSlide))]
        public float MoveOutOffset = -300f;
        [ShowField(nameof(UseSlide))]
        public float MoveInDuration = 0.5f;
        [ShowField(nameof(UseSlide))]
        public float MoveOutDuration = 0.5f;
        [ShowField(nameof(UseSlide))]
        public Ease MoveInEase = Ease.OutBack;
        [ShowField(nameof(UseSlide))]
        public Ease MoveOutEase = Ease.OutCirc;

        [Header("페이드 설정")]
        [ShowField(nameof(UseFade))]
        public float FadeInDuration = 0.5f;
        [ShowField(nameof(UseFade))]
        public float FadeOutDuration = 0.5f;
        [ShowField(nameof(UseFade))]
        public Ease FadeInEase = Ease.OutBack;
        [ShowField(nameof(UseFade))]
        public Ease FadeOutEase = Ease.OutCirc;

        [Header("공통 설정")]
        public bool WillPlayTogether = true;                // 설정한 애니메이션이 동시에 실행될 것인지
        public bool WillWaitUntilSeqEnd = false;            // 시퀀스가 끝나기 전이라도 다음 인터벌 대기 후 애니메이션 재생할지
        public bool LockInteractBeforeAnimEnd = false;      // 애니메이션이 끝나기 전까지 상호작용을 막을 것인지
        public float Interval = 0.05f;                      // 타겟별 순회 지연 시간
    }

    #endregion

    [RequireComponent(typeof(UIUnit))]
    [DisallowMultipleComponent]
    public class UIAnimator : MonoBehaviour
    {
        #region Fields and Properties

        [Header("애니메이션 그룹 등록")]
        [SerializeField] private List<UIGroup> _uiGroups;

        private readonly Dictionary<RectTransform, Vector2> _basePos = new();
        private bool _isCached = false;

        #endregion


        #region Convinience Methods

#if UNITY_EDITOR
        private bool _scheduledFix;

        private void OnValidate()
        {
            // 중복 예약 방지
            if (_scheduledFix) return;
            _scheduledFix = true;

            // 다음 에디터 업데이트 틱에서 실행
            EditorApplication.delayCall -= EnsureCanvasGroupsDeferred;
            EditorApplication.delayCall += EnsureCanvasGroupsDeferred;
        }

        private void EnsureCanvasGroupsDeferred()
        {
            _scheduledFix = false;

            // 객체가 파괴되었거나 Prefab 에셋 자체면 스킵
            if (this == null) return;
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;

            EnsureCanvasGroupsNow();
        }

        private void EnsureCanvasGroupsNow()
        {
            if (_uiGroups == null) return;

            foreach (var g in _uiGroups)
            {
                if (g?.Targets == null) continue;

                foreach (var t in g.Targets)
                {
                    if (t == null) continue;

                    if (!t.RectTransform && t.CanvasGroup)
                        t.RectTransform = t.CanvasGroup.transform as RectTransform;

                    if (t.RectTransform && !t.CanvasGroup)
                    {
                        var go = t.RectTransform.gameObject;
                        var cg = go.GetComponent<CanvasGroup>();
                        if (cg == null)
                            cg = Undo.AddComponent<CanvasGroup>(go);

                        t.CanvasGroup = cg;
                        EditorUtility.SetDirty(go);
                    }
                }
            }
            EditorUtility.SetDirty(this);
        }
#endif

        #endregion


        #region Public Methods

        public IEnumerator EnableAnimation(Action onComplete = null)
        {
            EnsureCache();

            foreach (var group in _uiGroups)
            {
                if (group == null || group.Targets == null) continue;

                foreach (var target in group.Targets)
                {
                    if (target == null || target.RectTransform == null || target.CanvasGroup == null) continue;

                    var rect = target.RectTransform;
                    var cg = target.CanvasGroup;

                    cg.gameObject.SetActive(true);

                    DOTween.Kill(cg);
                    DOTween.Kill(rect);

                    // 애니메이션이 없으면 바로 return
                    if (!group.UseSlide && !group.UseFade)
                    {
                        yield return new WaitForSecondsRealtime(group.Interval);
                        continue;
                    }

                    var seq = DOTween.Sequence()
                        .SetUpdate(true)
                        .SetLink(cg.gameObject, LinkBehaviour.KillOnDisable)
                        .OnComplete(() =>
                        {
                            cg.interactable = true;
                            cg.blocksRaycasts = true;
                        })
                        .OnKill(() =>
                        {
                            cg.interactable = true;
                            cg.blocksRaycasts = true;
                        });

                    if (group.LockInteractBeforeAnimEnd)
                    {
                        cg.interactable = false;
                        cg.blocksRaycasts = false;
                    }

                    if (group.UseSlide)
                    {
                        //rect.anchoredPosition = OffsetFromBase(rect, group.SlideDirection, group.MoveInOffset);
                        //var slide = rect.DOAnchorPos(OffsetFromBase(rect, group.SlideDirection, group.OriginOffset),
                        //                             group.MoveInDuration)
                        //                .SetEase(group.MoveInEase)
                        //                .SetUpdate(true)
                        //                .SetLink(rect.gameObject, LinkBehaviour.KillOnDisable);

                        //if (group.WillPlayTogether) seq.Join(slide);
                        //else                        seq.Append(slide);
                    }

                    if (group.UseFade)
                    {
                        //cg.alpha = 0f;
                        //var fade = cg.DOFade(1f, group.FadeInDuration)
                        //            .SetEase(group.FadeInEase)
                        //            .SetUpdate(true)
                        //            .SetLink(cg.gameObject, LinkBehaviour.KillOnDisable);

                        //if (group.WillPlayTogether) seq.Join(fade);
                        //else                        seq.Append(fade);
                    }

                    if (group.WillWaitUntilSeqEnd)
                    {
                        yield return seq.WaitForCompletion();
                    }
                    yield return new WaitForSecondsRealtime(group.Interval);
                }
            }
            onComplete?.Invoke();
        }

        public IEnumerator DisableAnimation(Action onComplete = null)
        {
            EnsureCache();

            foreach (var group in _uiGroups)
            {
                if (group == null || group.Targets == null) continue;

                foreach (var t in group.Targets)
                {
                    if (t == null || t.RectTransform == null || t.CanvasGroup == null) continue;

                    var rect = t.RectTransform;
                    var cg = t.CanvasGroup;

                    DOTween.Kill(cg);
                    DOTween.Kill(rect);

                    // 애니메이션 중 상호작용 허용 안하니까 꺼지기 전에 그냥 미리 상호작용 종료
                    if (group.LockInteractBeforeAnimEnd)
                    {
                        cg.interactable = false;
                        cg.blocksRaycasts = false;
                    }

                    var seq = DOTween.Sequence()
                        .SetUpdate(true)
                        .SetLink(cg.gameObject, LinkBehaviour.KillOnDisable)
                        .OnComplete(() =>
                        {
                            cg.interactable = true;
                            cg.blocksRaycasts = true;
                        })
                        .OnKill(() =>
                        {
                            cg.interactable = true;
                            cg.blocksRaycasts = true;
                        });

                    if (group.UseSlide)
                    {
                        //var slide = rect.DOAnchorPos(OffsetFromBase(rect, group.SlideDirection, group.MoveOutOffset),
                        //                             group.MoveOutDuration)
                        //                .SetEase(group.MoveOutEase)
                        //                .SetUpdate(true)
                        //                .SetLink(rect.gameObject, LinkBehaviour.KillOnDisable);

                        //if (group.WillPlayTogether) seq.Join(slide);
                        //else                        seq.Append(slide);
                    }

                    if (group.UseFade)
                    {
                        //var fade = cg.DOFade(0f, group.FadeOutDuration)
                        //            .SetEase(group.FadeOutEase)
                        //            .SetUpdate(true)
                        //            .SetLink(cg.gameObject, LinkBehaviour.KillOnDisable);

                        //if (group.WillPlayTogether) seq.Join(fade);
                        //else                        seq.Append(fade);
                    }

                    if (group.WillWaitUntilSeqEnd)
                    {
                        yield return seq.WaitForCompletion();
                    }
                    yield return new WaitForSecondsRealtime(group.Interval);
                }
            }
            onComplete?.Invoke();
        }

        /// <summary>
        /// 런타임에 UI 변경되었다면 캐시 리셋을 해주기 위해 호출해야 합니다.
        /// </summary>
        public void Recache()
        {
            _basePos.Clear();
            _isCached = false;
        }

        #endregion


        #region Private Methods

        private void EnsureCache()
        {
            if (_isCached) return;

            if (_uiGroups == null) return;

            Canvas.ForceUpdateCanvases();

            foreach (var g in _uiGroups)
            {
                if (g == null || g.Targets == null) continue;
                foreach (var t in g.Targets)
                {
                    if (t == null || t.RectTransform == null) continue;
                    var rect = t.RectTransform;
                    if (!_basePos.ContainsKey(rect))
                        _basePos[rect] = rect.anchoredPosition;
                }
            }
            _isCached = true;
        }

        // 부모 RectTransform 기준 픽셀 이동
        private Vector2 OffsetFromBase(RectTransform rect, SlideDirection dir, float offset)
        {
            Vector2 b = _basePos.TryGetValue(rect, out var p) ? p : rect.anchoredPosition;

            if (dir == SlideDirection.Horizontal) return new Vector2(b.x + offset, b.y);
            else return new Vector2(b.x, b.y + offset);
        }

        #endregion
    }
}

