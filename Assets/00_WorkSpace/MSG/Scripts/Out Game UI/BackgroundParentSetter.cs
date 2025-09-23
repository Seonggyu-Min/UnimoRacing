using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundParentSetter : MonoBehaviour
{
    [SerializeField] private GameObject _backgroundParent;      // 배경용 부모
    [SerializeField] private GameObject _backgroundSelf;        // 옮길 대상, null이면 자기 자신 사용

    private bool _isCached = false;
    private Transform _originalParent;
    private int _originalSiblingIndex;

    private Transform _reparentedTo;

    private GameObject BackgroundSelf => _backgroundSelf != null ? _backgroundSelf : gameObject;


    private void OnEnable()
    {
        SetParentForBackground();
    }

    private void OnDisable()
    {
        SetParentForOrigin();
    }

    private void CacheOriginalStateIfNeeded()
    {
        if (_isCached) return;

        var t = BackgroundSelf.transform;
        _originalParent = t.parent;
        _originalSiblingIndex = t.GetSiblingIndex();

        _isCached = true;
    }

    private void SetParentForBackground()
    {
        CacheOriginalStateIfNeeded();

        if (_backgroundParent == null) return;

        var t = BackgroundSelf.transform;

        // 이미 해당 부모면 스킵
        if (t.parent == _backgroundParent.transform) return;

        t.SetParent(_backgroundParent.transform, false);
        _reparentedTo = _backgroundParent.transform;
    }

    private void SetParentForOrigin()
    {
        if (_reparentedTo != null && _originalParent != null)
        {
            var t = BackgroundSelf.transform;
            t.SetParent(_originalParent, false);
            t.SetSiblingIndex(_originalSiblingIndex);
        }

        _reparentedTo = null;
    }
}
