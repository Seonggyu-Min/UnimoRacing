using System;

public enum StatusOptionType
{
    None,
    Speed,
}

[Serializable]
public class SkillOption
{
    public StatusOptionType optionType;

    public int optionValue;
    public float optionDuration;
}