using System.Collections.Generic;

public class EffectBucket
{
    /// <summary>
    /// 한 버킷은 오직 하나의 StatusEffect 타입만 관리.
    /// </summary>
    public readonly StatusEffect Effect;

    /// <summary>
    /// 실제로 적용 중인 효과 인스턴스 리스트.
    /// </summary>
    public readonly List<StatusEffectInstance> Instances = new(4);

    /// <summary>
    /// 합산 값(ResultValue, ResultPercent)이 최신인지 여부. false면 다시 계산.
    /// </summary>
    public bool Dirty = true;

    /// <summary>
    /// 모든 인스턴스의 "절대값 가감" 합산 결과. 
    /// 효과 룰의 정책에 맞게 처리됩니다.
    /// </summary>
    public float ResultValue;

    /// <summary>
    /// 모든 인스턴스의 "퍼센트 가감" 합산 결과.
    /// 효과 룰의 정책에 맞게 처리됩니다.
    /// </summary>
    public float ResultPercent;

    /// <summary>
    /// 생성자.
    /// 새로운 버킷을 만들 때 어떤 StatusEffect를 관리할지 지정.
    /// </summary>
    public EffectBucket(StatusEffect effect) => Effect = effect;

    /// <summary>
    /// 현재 Instances에 들어있는 모든 인스턴스를 값을 다시 결과 계산한다. 
    /// </summary>
    public void Recalculate()
    {
        float v = 0f, p = 0f;
        for (int i = 0; i < Instances.Count; i++)
        {
            v += Instances[i].value;
            p += Instances[i].percent;
        }
        ResultValue = v;
        ResultPercent = p;
        Dirty = false;
    }
}