using System;

namespace Core.UnityUtil.PoolTool
{
    public interface IPoolable
    {
        Action OnSpawn { get; set; }
        Action OnDespawn { get; set; }

        void OnSpawned();
        void OnDespawned();
    }
}
