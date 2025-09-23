using System;

public enum StatusEffect
{
    Slow,           // 이동속도 감소
    Haste,          // 이동속도 가속
    Slip,           // 미끄러짐
    Silence,        // 침묵
    Immunity,       // 면역
    Airborne,       // 에어본
    Root,           // 속박
    Blind           // 시야 차단
}

[Serializable]
public class StatusEffectOption
{
    // 적용 시킬 효과
    public StatusEffect optionStatusEffect;

    // 값, 퍼센트, 지속 시간
    public float optionValue;
    public float optionPercent;
    public float optionDuration;
}