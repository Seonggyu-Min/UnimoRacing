using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class StatusEffectSystem : MonoBehaviour
{
    [Header("효과 만료 체크")]
    [SerializeField] private float _expireCheckInterval = 0.1f;

    [Header("상태 효과 룰 (=> 선택 사항)")]
    [SerializeField] private StatusEffectRulesSO _rules;

    private readonly Dictionary<StatusEffect, EffectBucket> _effects = new();
    private readonly List<StatusEffect> _tmpDirtyEffects = new(8);
    private double _nextExpireCheck;

    public event Action<StatusEffectInstance> OnApplied;                // 적용
    public event Action<StatusEffectInstance> OnRefreshedOrStacked;
    public event Action<StatusEffectInstance> OnExpired;                // 해체
    public event Action<StatusEffect> OnAggregatesChanged;              // 집계 변경

    private static double Now => PhotonNetwork.Time;

    /// <summary>
    /// 아이템으로 효과 적용
    /// </summary>
    /// <param name="item">아이템 SO</param>
    /// <param name="sourceActor">적용 대상</param>
    public void ApplyFromItem(UnimoItemSO item, int sourceActor = -1)
    {
        if (item == null || item.options == null) return;

        foreach (var opt in item.options)
        {
            var inst = new StatusEffectInstance
            {
                effect = opt.optionStatusEffect,
                value = opt.optionValue,
                percent = opt.optionPercent,
                duration = Mathf.Max(0f, item.itemEffectDuration),
                startTime = Now,
                sourceActor = sourceActor
            };
            Apply(inst, item.reapplyMode, Mathf.Max(1, (int)item.stackCount), compareByPercentFirst: false);
        }

    }
    /// <summary>
    /// 규칙에 맞는 효과 적용
    /// </summary>
    /// <param name="inst">효과</param>
    /// <param name="mode">재적용 규칙</param>
    /// <param name="maxStacks">최대 스택 수</param>
    /// <param name="compareByPercentFirst">강도 비교 기준 우선순위(true면 Percent 우선, false면 Value 우선)</param>
    private void Apply(StatusEffectInstance inst, ReapplyMode mode, int maxStacks, bool compareByPercentFirst)
    {
        var e = inst.effect;

        if (!_effects.TryGetValue(e, out var bucket))
        {
            bucket = new EffectBucket(e);
            _effects[e] = bucket;
        }

        ApplyExclusiveRulesBefore(e, inst);

        bool changed = false;

        switch (mode)
        {
            // 재적용 시 지속시간 갱신
            case ReapplyMode.RefreshDuration:
                if (bucket.Instances.Count > 0)
                {
                    int idx = SelectTargetIndex(bucket, compareByPercentFirst);
                    var target = bucket.Instances[idx];
                    target.startTime = Now;
                    target.duration = inst.duration;
                    bucket.Instances[idx] = target;
                    OnRefreshedOrStacked?.Invoke(target);
                }
                else
                {
                    bucket.Instances.Add(inst);
                    OnApplied?.Invoke(inst);
                }
                changed = true;
                break;

            // 재적용 시 지속시간 누적
            case ReapplyMode.AddDuration:
                if (bucket.Instances.Count == 0)
                {
                    bucket.Instances.Add(inst);
                    OnApplied?.Invoke(inst);
                }
                else
                {
                    int idx = SelectTargetIndex(bucket, compareByPercentFirst);
                    var target = bucket.Instances[idx];
                    float remaining = target.Remaining(Now);
                    target.startTime = Now;
                    target.duration = remaining + inst.duration;
                    bucket.Instances[idx] = target;
                    OnRefreshedOrStacked?.Invoke(target);
                }
                changed = true;
                break;

            // 활성 중이면 무시
            case ReapplyMode.IgnoreIfActive:
                if (bucket.Instances.Count == 0)
                {
                    bucket.Instances.Add(inst);
                    OnApplied?.Invoke(inst);
                    changed = true;
                }
                break;

            // 더 강하면 교체(값/퍼센트 기준)
            case ReapplyMode.ReplaceIfStronger:
                if (bucket.Instances.Count == 0)
                {
                    bucket.Instances.Add(inst);
                    OnApplied?.Invoke(inst);
                    changed = true;
                }
                else
                {
                    int idx = SelectTargetIndex(bucket, compareByPercentFirst);
                    var target = bucket.Instances[idx];
                    bool stronger = IsStronger(inst, target, compareByPercentFirst);
                    if (stronger)
                    {
                        bucket.Instances[idx] = inst;
                        OnRefreshedOrStacked?.Invoke(inst);
                        changed = true;
                    }
                }
                break;
        }

        if (changed && maxStacks > 1)
            TrimStacks(bucket, maxStacks, compareByPercentFirst);

        if (changed)
        {
            bucket.Dirty = true;
            OnAggregatesChanged?.Invoke(e);
        }
    }
    /// <summary>
    /// 배타적 규칙을 적용
    /// </summary>
    /// <param name="e"></param>
    /// <param name="incoming"></param>
    private void ApplyExclusiveRulesBefore(StatusEffect e, in StatusEffectInstance incoming)
    {
        // 룰SO 체크
        if (_rules == null) return;

        // 효과 룰
        var rule = _rules.Get(e);

        // 효과 룰, 효과와 같이 사용될 수 없는 리스트(효과 배타 규칙)
        if (rule == null || rule.exclusiveWith == null || rule.exclusiveWith.Count == 0) return;

        for (int i = 0; i < rule.exclusiveWith.Count; i++)
        {
            // 배타 효과
            var ex = rule.exclusiveWith[i];

            // 적용 효과 버킷에 > 적용 중인 효과 리스트 있는지
            if (_effects.TryGetValue(ex, out var other) && other.Instances.Count > 0)
            {
                // 강한 효과 선호 여부
                if (rule.preferStronger)
                {
                    int idx = SelectTargetIndex(other, true);
                    var strongest = other.Instances[idx]; 
                    bool incomingStronger = IsStronger(incoming, strongest, true);
                    if (incomingStronger)
                    {
                        other.Instances.Clear();
                        other.Dirty = true;
                        OnAggregatesChanged?.Invoke(ex);
                    }
                }
                else
                {
                    other.Instances.Clear();
                    other.Dirty = true;
                    OnAggregatesChanged?.Invoke(ex);
                }
            }
        }
    }

    /// <summary>
    /// 효과 보유 확인
    /// </summary>
    /// <param name="e">보유 중인지 확인하고 싶은 효과</param>
    /// <returns></returns>
    public bool Has(StatusEffect e) => _effects.TryGetValue(e, out var b) && b.Instances.Count > 0;
    /// <summary>
    /// 효과 최종 값 가져오기
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public (float value, float percent) GetTotals(StatusEffect e)
    {
        if (!_effects.TryGetValue(e, out var b) || b.Instances.Count == 0)
            return (0f, 0f);
        if (b.Dirty) b.Recalculate();
        return (b.ResultValue, b.ResultPercent);
    }
    /// <summary>
    /// 적용 효과 중, 잔여 시간 가장 높은 시간 가져오기
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public float GetRemainingMax(StatusEffect e)
    {
        if (!_effects.TryGetValue(e, out var b)) return 0f;
        double now = Now;
        float max = 0f;
        for (int i = 0; i < b.Instances.Count; i++)
            max = Mathf.Max(max, b.Instances[i].Remaining(now));
        return max;
    }
    /// <summary>
    /// 효과 제거
    /// </summary>
    /// <param name="e"></param>
    public void Remove(StatusEffect e)
    {
        if (!_effects.TryGetValue(e, out var b)) return;
        b.Instances.Clear();
        b.Dirty = true;
        OnAggregatesChanged?.Invoke(e);
    }
    /// <summary>
    /// 모든 효과 제거
    /// </summary>
    public void RemoveAll()
    {
        foreach (var eb in _effects)
            Remove(eb.Key);
    }



    /// <summary>
    /// 현재 모든 상태효과 가져오기
    /// 넷 코드 사용
    /// </summary>
    /// <returns></returns>
    public List<StatusEffectInstance> GetAllInstances()
    {
        var list = new List<StatusEffectInstance>(32);
        foreach (var kv in _effects)
            list.AddRange(kv.Value.Instances);
        return list;
    }
    /// <summary>
    /// 상태효과 인스턴스를 규칙 로직(ReapplyMode, 스택 제한, 강약 비교 등)을 거치지 않고
    /// 강제로 버킷에 삽입. 보통, 넷 코드로 받아온 인스턴스를 강제로 주입.
    /// </summary>
    public void ForceAddInstance(StatusEffectInstance inst)
    {
        if (!_effects.TryGetValue(inst.effect, out var b))
            _effects[inst.effect] = b = new EffectBucket(inst.effect);
        b.Instances.Add(inst);
        b.Dirty = true;
        OnAggregatesChanged?.Invoke(inst.effect);
    }



    private void Update()
    {
        // 서버 시간 받아서 갱신 타이밍 체크
        double now = Now;
        if (now < _nextExpireCheck) return;
        _nextExpireCheck = now + _expireCheckInterval;

        _tmpDirtyEffects.Clear();

        foreach (var kv in _effects)
        {
            var e = kv.Key;
            var b = kv.Value;
            bool removedAny = false;

            // 효과 만료 체크
            for (int i = b.Instances.Count - 1; i >= 0; i--)
            {
                var inst = b.Instances[i];
                if (now >= inst.EndTime)
                {
                    b.Instances.RemoveAt(i);
                    removedAny = true;
                    OnExpired?.Invoke(inst);
                }
            }

            // 한개라도 만료 있다면
            if (removedAny)
            {
                b.Dirty = true;
                _tmpDirtyEffects.Add(e);
            }
        }

        // 집계 변경 이벤트
        for (int i = 0; i < _tmpDirtyEffects.Count; i++)
            OnAggregatesChanged?.Invoke(_tmpDirtyEffects[i]);
    }



    #region Util

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bucket"></param>
    /// <param name="byPercentFirst"></param>
    /// <returns></returns>
    private int SelectTargetIndex(EffectBucket bucket, bool byPercentFirst)
    {
        int best = 0;
        for (int i = 1; i < bucket.Instances.Count; i++)
        {
            if (IsStronger(bucket.Instances[i], bucket.Instances[best], byPercentFirst))
                best = i;
        }

        return best;
    }

    private bool IsStronger(in StatusEffectInstance a, in StatusEffectInstance b, bool byPercentFirst)
    {
        // Mathf.Epsilon
        // float 타입에서 표현할 수 있는 가장 작은 양수
        if (byPercentFirst)
        { 
            if (Mathf.Abs(a.percent - b.percent) > Mathf.Epsilon) return a.percent > b.percent;
            if (Mathf.Abs(a.value - b.value) > Mathf.Epsilon) return a.value > b.value;
        }
        else
        {
            if (Mathf.Abs(a.value - b.value) > Mathf.Epsilon) return a.value > b.value;
            if (Mathf.Abs(a.percent - b.percent) > Mathf.Epsilon) return a.percent > b.percent;
        }
        return a.duration >= b.duration;
    }

    private void TrimStacks(EffectBucket bucket, int maxStacks, bool byPercentFirst)
    {
        if (bucket.Instances.Count <= maxStacks) return;

        bucket.Instances.Sort((x, y) =>
        {
            bool xs = IsStronger(x, y, byPercentFirst);
            bool ys = IsStronger(y, x, byPercentFirst);
            if (xs == ys) return 0;
            return xs ? -1 : 1;
        });

        while (bucket.Instances.Count > maxStacks)
            bucket.Instances.RemoveAt(bucket.Instances.Count - 1);
    }

    #endregion
}