using System;



[Serializable]
public struct StatusEffectOption
{
    // 적용 시킬 효과
    public StatusEffect optionStatusEffect;

    // 값, 퍼센트, 지속 시간
    public float optionValue;
    public float optionPercent;
}