using System.Collections.Generic;
using UnityEngine;
using YSJ.Util;

public class UnimoKartAniCtrl : MonoBehaviour
{
    private readonly int IS_STUN_ID     = Animator.StringToHash("isstun");      // 스턴을 넣을지 여부
    private readonly int IS_MOVING_ID   = Animator.StringToHash("ismoving");    // 이동 여부
    private readonly int STUN_ID        = Animator.StringToHash("stun");        // 스턴
    private readonly int DISAPPEAR_ID   = Animator.StringToHash("disappear");   // 사라짐
    private readonly int MOVE_SYNC_ID   = Animator.StringToHash("movesync");    // 이동 동기화

    private Animator _animator;

    private readonly Dictionary<int, AnimatorControllerParameterType> _paramMap
        = new Dictionary<int, AnimatorControllerParameterType>(8);

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

    #region Safe Set
    public bool SetBoolIsStun(bool isStun = true)
    {
        return SetBoolSafe(IS_STUN_ID, isStun);
    }

    // 주요 공통 사항
    public bool SetBoolIsMoving(bool isMoving = true)
    {
        return SetBoolSafe(IS_MOVING_ID, isMoving);
    }

    public bool SetTriggerStun()
    {
        return SetTriggerSafe(STUN_ID);
    }

    public bool SetTriggerDisappear()
    {
        return SetTriggerSafe(DISAPPEAR_ID);
    }

    public bool SetFloatMovesync(float movesync = 1.0f)
    {
        return SetFloatSafe(MOVE_SYNC_ID, movesync);
    }
    #endregion

    #region Safe Get
    public bool GetBoolIsStun()
    {
        return GetBoolSafe(IS_STUN_ID, false);
    }

    // 주요 공통 사항
    public bool GetBoolIsMoving()
    {
        return GetBoolSafe(IS_MOVING_ID, false);
    }

    public float GetFloatMovesync()
    {
        return GetFloatSafe(MOVE_SYNC_ID, 1.0f);
    }
    #endregion

    #region Contains / Safe Utils
    /// <summary>
    /// 파라미터가 존재 여부 확인
    /// </summary>
    public bool ContainsParam(int nameHash)
    {
        // AnimatorController 바뀔 수 있으니 null 방어(혹시 모르니)
        if (_animator == null) return false;
        return _paramMap.ContainsKey(nameHash);
    }

    /// <summary>
    /// 파라미터가 특정 타입으로 존재 여뷰
    /// </summary>
    public bool ContainsParam(int nameHash, AnimatorControllerParameterType type)
    {
        if (_animator == null) return false;
        return _paramMap.TryGetValue(nameHash, out var t) && t == type;
    }

    /// <summary>
    /// 파라미터(문자열)가 존재 여부 
    /// </summary>
    public bool ContainsParam(string paramName)
    {
        if (string.IsNullOrEmpty(paramName)) return false;
        return ContainsParam(Animator.StringToHash(paramName));
    }

    public bool SetBoolSafe(int nameHash, bool value)
    {
        if (!ContainsParam(nameHash, AnimatorControllerParameterType.Bool))
        {
            this.PrintLog($"Bool 파라미터 없음 (hash:{nameHash})");
            return false;
        }
        _animator.SetBool(nameHash, value);
        return true;
    }

    public bool SetTriggerSafe(int nameHash)
    {
        if (!ContainsParam(nameHash, AnimatorControllerParameterType.Trigger))
        {
            Debug.LogWarning($"Trigger 파라미터 없음 (hash:{nameHash})");
            return false;
        }
        _animator.SetTrigger(nameHash);
        return true;
    }

    public bool SetFloatSafe(int nameHash, float value)
    {
        if (!ContainsParam(nameHash, AnimatorControllerParameterType.Float))
        {
            Debug.LogWarning($"Float 파라미터 없음 (hash:{nameHash})");
            return false;
        }
        _animator.SetFloat(nameHash, value);
        return true;
    }

    public bool GetBoolSafe(int nameHash, bool defaultValue)
    {
        if (!ContainsParam(nameHash, AnimatorControllerParameterType.Bool)) return defaultValue;
        return _animator.GetBool(nameHash);
    }

    public float GetFloatSafe(int nameHash, float defaultValue)
    {
        if (!ContainsParam(nameHash, AnimatorControllerParameterType.Float)) return defaultValue;
        return _animator.GetFloat(nameHash);
    }
    #endregion
}
