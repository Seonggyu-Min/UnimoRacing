using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSG
{
    [Serializable]
    public class IapRewardEntry
    {
        public string ProductId;
        public int BlueAmount;
        public Sprite Sprite;
    }


    [CreateAssetMenu(fileName = "IAPTable", menuName = "ScriptableObjects/IAPTable")]
    public class IAPTable : ScriptableObject
    {
        public List<IapRewardEntry> Entries = new();
        private Dictionary<string, int> _map;

        public void Build()
        {
            _map = new();
            foreach (var e in Entries)
            {
                _map[e.ProductId] = e.BlueAmount;
            }
        }

        public bool TryGet(string productId, out int amount)
        {
            if (_map == null)
            {
                Build();
            }
            return _map.TryGetValue(productId, out amount);
        }
    }
}
