using System;

public enum StatusOptionType
{
    None,
    Speed,
    Targeting_Item_Defense, 
    Obstacle_Item_Defense,
    All_Defense,
}

[Flags]
public enum ApplyTarget
{
    None,           // 대상 없음
    Self,           // 시전자 자신
    Ally,           // 아군 단일
    AllyAll,        // 아군 전체
    Enemy,          // 적 단일
    EnemyAll,       // 적 전체
    Area,           // 지정된 범위
    RandomEnemy,    // 무작위 적
}

[Serializable]
public class SkillOption
{
    public ApplyTarget applyTarget;
    public StatusOptionType optionType;
    public int optionValue;
    public float optionDuration;
}