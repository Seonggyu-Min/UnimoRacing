using System.Collections.Generic;
using UnityEngine;
using YSJ.Util;

public class UnimoCharacterAniCtrl : MonoBehaviour
{
    private readonly int BLINK_ID  = Animator.StringToHash("blink");   // 눈 깜빡임 (Trigger)
    private readonly int SHAKE1_ID = Animator.StringToHash("shake1");  // 귀 돌리기1 (Trigger)
    private readonly int SHAKE2_ID = Animator.StringToHash("shake2");  // 귀 돌리기2 (Trigger)

    private Animator _animator;

    // 파라미터 캐시
    private readonly Dictionary<int, AnimatorControllerParameterType> _paramMap
        = new Dictionary<int, AnimatorControllerParameterType>(4);

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        RefreshParamCache();
    }

    private void RefreshParamCache()
    {
        _paramMap.Clear();
        if (_animator == null) return;

        var ps = _animator.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            _paramMap[ps[i].nameHash] = ps[i].type;
        }
    }

    #region Safe Set (Triggers)
    public bool SetTriggerBlink()
    {
        return SetTriggerSafe(BLINK_ID);
    }

    public bool SetTriggerShake1()
    {
        return SetTriggerSafe(SHAKE1_ID);
    }

    public bool SetTriggerShake2()
    {
        return SetTriggerSafe(SHAKE2_ID);
    }
    #endregion

    #region Contains / Safe Utils
    /// <summary>
    /// 파라미터 존재 여부 (해시)
    /// </summary>
    public bool ContainsParam(int nameHash)
    {
        if (_animator == null) return false;
        return _paramMap.ContainsKey(nameHash);
    }

    /// <summary>
    /// 파라미터가 특정 타입 확인용
    /// </summary>
    public bool ContainsParam(int nameHash, AnimatorControllerParameterType type)
    {
        if (_animator == null) return false;
        return _paramMap.TryGetValue(nameHash, out var t) && t == type;
    }

    /// <summary>
    /// 파라미터 존재 여부 (문자열)
    /// </summary>
    public bool ContainsParam(string paramName)
    {
        if (string.IsNullOrEmpty(paramName)) return false;
        return ContainsParam(Animator.StringToHash(paramName));
    }

    public bool SetTriggerSafe(int nameHash)
    {
        if (!ContainsParam(nameHash, AnimatorControllerParameterType.Trigger))
        {
            this.PrintLog($"[UnimoCharacterAniCtrl] Trigger 파라미터 없음 (hash:{nameHash})");
            return false;
        }
        _animator.SetTrigger(nameHash);
        return true;
    }
    #endregion
}
