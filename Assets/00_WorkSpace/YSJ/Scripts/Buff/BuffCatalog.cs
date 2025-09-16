using System.Collections.Generic;

public static class BuffCatalog
{
    private static readonly Dictionary<BuffId, (BuffCategory cat, BuffStackPolicy policy, float baseDuration)> _defs
        = new Dictionary<BuffId, (BuffCategory, BuffStackPolicy, float)>
    {
        { BuffId.Nitro,        (BuffCategory.Speed,    BuffStackPolicy.Replace, 3.0f) },
        { BuffId.Shield,       (BuffCategory.Defense,  BuffStackPolicy.Replace, 10.0f) },
        { BuffId.Magnet,       (BuffCategory.Utility,  BuffStackPolicy.Replace, 6.0f) },
        { BuffId.TrapImmunity, (BuffCategory.Defense,  BuffStackPolicy.Replace, 5.0f) },
        { BuffId.Slipstream,   (BuffCategory.Speed,    BuffStackPolicy.Replace, 2.5f) },
    };

    public static bool TryGet(BuffId id, out BuffCategory cat, out BuffStackPolicy policy, out float baseDuration)
    {
        if (_defs.TryGetValue(id, out var t))
        {
            cat = t.cat; policy = t.policy; baseDuration = t.baseDuration;
            return true;
        }
        cat = default; policy = default; baseDuration = 0f;
        return false;
    }
}
