using System;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// 상태 효과 룰
/// </summary>
[Serializable]
public class StatusEffectRuleData
{
    /// <summary>
    /// 규칙이 적용되는 주 대상 효과
    /// </summary>
    public StatusEffect effect;

    [Tooltip("서로 공존할 수 없는 다른 효과")]
    public List<StatusEffect> exclusiveWith = new();
    [Tooltip("효과 적용 시, 더 강한 쪽을 남길지 여부(true=강한 쪽, false=마지막 적용)")]
    public bool preferStronger = true;
}