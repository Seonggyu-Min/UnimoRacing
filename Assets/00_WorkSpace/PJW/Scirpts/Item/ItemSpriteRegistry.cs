using System.Collections.Generic;
using UnityEngine;


namespace PJW
{   
    public class ItemSpriteRegistry : MonoBehaviour
    {
        private static ItemSpriteRegistry instance;
        public static ItemSpriteRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject(nameof(ItemSpriteRegistry));
                    instance = go.AddComponent<ItemSpriteRegistry>();
                    Object.DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private readonly Dictionary<string, Sprite> iconMap = new Dictionary<string, Sprite>();

        public Sprite GetIcon(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return null;
            iconMap.TryGetValue(prefabName, out var icon);
            return icon;
        }

        public void RegisterIcon(string prefabName, Sprite icon)
        {
            if (string.IsNullOrEmpty(prefabName) || icon == null) return;
            iconMap[prefabName] = icon;
        }
    }
}
