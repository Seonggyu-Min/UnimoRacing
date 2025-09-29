using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStatusEffectRulesSO", menuName = "Game/StatusEffectRules")]
public class StatusEffectRulesSO : ScriptableObject
{
    public List<StatusEffectRuleData> rules = new();
    public StatusEffectRuleData Get(StatusEffect e) => rules.Find(r => r.effect == e);

    private void OnValidate()
    {
        foreach (var r in rules)
        {
            if (r == null || r.exclusiveWith == null)
                continue;

            int findIndex = r.exclusiveWith.IndexOf(r.effect);
            if (findIndex != -1)
                r.exclusiveWith.RemoveAt(findIndex);
        }
    }
}