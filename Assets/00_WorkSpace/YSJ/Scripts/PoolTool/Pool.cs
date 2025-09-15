using System.Collections.Generic;

using UnityEngine;

namespace Core.UnityUtil.PoolTool
{
    public class Pool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Stack<GameObject> _poolStack = new Stack<GameObject>();

        private GameObject _group;

        public Pool(GameObject prefab, int initialSize, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;

            _group = new GameObject($"Group [{prefab.name}]");
            _group.transform.parent = parent;

            CreatePool(prefab, initialSize);
        }

        public GameObject Get()
        {
            GameObject obj = _poolStack.Count > 0
                ? _poolStack.Pop()
                : GameObject.Instantiate(_prefab, _parent);

            obj.SetActive(true);
            foreach (var comp in obj.GetComponents<IPoolable>())
                comp.OnSpawned();

            return obj;
        }

        public void Release(GameObject obj)
        {
            foreach (var comp in obj.GetComponents<IPoolable>())
                comp.OnDespawned();

            obj.SetActive(false);
            _poolStack.Push(obj);
        }

        public void CreatePool(GameObject prefab, int initialSize)
        {
            int index = (_poolStack.Count - 1 <= 0) ? 0 : _poolStack.Count - 1;

            for (int i = index; i < initialSize; i++)
            {
                GameObject obj = GameObject.Instantiate(prefab, _group.transform);
                obj.SetActive(false);
                _poolStack.Push(obj);
            }
        }

        public void Clear(bool destroyGroup = true)
        {
            while (_poolStack.Count > 0)
            {
                var obj = _poolStack.Pop();
                if (obj != null)
                    GameObject.Destroy(obj);
            }

            if (_group != null)
            {
                for (int i = _group.transform.childCount - 1; i >= 0; i--)
                {
                    var child = _group.transform.GetChild(i);
                    if (child != null)
                        GameObject.Destroy(child.gameObject);
                }
            }

            if (destroyGroup && _group != null)
            {
                GameObject.Destroy(_group);
                _group = null;
            }
        }
    }
}
