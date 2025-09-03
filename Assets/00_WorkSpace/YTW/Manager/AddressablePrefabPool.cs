using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace YTW
{
    /// <summary>
    /// Addressables �ý��۰� �����Ǵ� Ŀ���� Photon Prefab Pool�Դϴ�.
    /// PhotonNetwork.Instantiate�� ȣ��� ��, �̸� �ε�� ��巹���� �������� ����մϴ�.
    /// </summary>
    public class AddressablePrefabPool : IPunPrefabPool
    {
        // �̸� �ε�� �����յ��� �����ϴ� ��ųʸ�.
        // Key: ��巹���� �ּ� (string), Value: �ε�� ������ ���� (GameObject)
        private readonly Dictionary<string, GameObject> _preloadedPrefabs = new Dictionary<string, GameObject>();

        /// <summary>
        /// ��巹����� �ε��� �������� �� Ǯ�� ����մϴ�.
        /// ���� ���� �����ϱ� ���� �ʿ��� ��� ��Ʈ��ũ �������� ����ؾ� �մϴ�.
        /// </summary>
        public void RegisterPrefab(string prefabId, GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError($"[AddressablePrefabPool] '{prefabId}' �ּ��� �������� null�Դϴ�. ����� �� �����ϴ�.");
                return;
            }
            if (!_preloadedPrefabs.ContainsKey(prefabId))
            {
                _preloadedPrefabs.Add(prefabId, prefab);
                Debug.Log($"[AddressablePrefabPool] '{prefabId}' �������� ���������� ��ϵǾ����ϴ�.");
            }
        }

        /// <summary>
        /// PhotonNetwork.Instantiate�� ȣ��� �� ����Ǵ� �Լ��Դϴ�.
        /// </summary>
        public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
        {
            if (!_preloadedPrefabs.TryGetValue(prefabId, out GameObject prefab))
            {
                Debug.LogError($"[AddressablePrefabPool] '{prefabId}' �ּ��� �������� �̸� �ε���� �ʾҽ��ϴ�! Instantiate ����.");
                return null;
            }

            // ��ϵ� �������� ����ǰ�� �����Ͽ� ��ȯ�մϴ�.
            return Object.Instantiate(prefab, position, rotation);
        }

        /// <summary>
        /// PhotonNetwork.Destroy�� ȣ��� �� ����Ǵ� �Լ��Դϴ�.
        /// </summary>
        public void Destroy(GameObject gameObject)
        {
            Object.Destroy(gameObject);
        }
    }
}
