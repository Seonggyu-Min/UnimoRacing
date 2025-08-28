using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace YTW
{
    /// <summary>
    /// Addressables 시스템과 연동되는 커스텀 Photon Prefab Pool입니다.
    /// PhotonNetwork.Instantiate가 호출될 때, 미리 로드된 어드레서블 프리팹을 사용합니다.
    /// </summary>
    public class AddressablePrefabPool : IPunPrefabPool
    {
        // 미리 로드된 프리팹들을 저장하는 딕셔너리.
        // Key: 어드레서블 주소 (string), Value: 로드된 프리팹 원본 (GameObject)
        private readonly Dictionary<string, GameObject> _preloadedPrefabs = new Dictionary<string, GameObject>();

        /// <summary>
        /// 어드레서블로 로드한 프리팹을 이 풀에 등록합니다.
        /// 게임 씬에 진입하기 전에 필요한 모든 네트워크 프리팹을 등록해야 합니다.
        /// </summary>
        public void RegisterPrefab(string prefabId, GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError($"[AddressablePrefabPool] '{prefabId}' 주소의 프리팹이 null입니다. 등록할 수 없습니다.");
                return;
            }
            if (!_preloadedPrefabs.ContainsKey(prefabId))
            {
                _preloadedPrefabs.Add(prefabId, prefab);
                Debug.Log($"[AddressablePrefabPool] '{prefabId}' 프리팹이 성공적으로 등록되었습니다.");
            }
        }

        /// <summary>
        /// PhotonNetwork.Instantiate가 호출될 때 실행되는 함수입니다.
        /// </summary>
        public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
        {
            if (!_preloadedPrefabs.TryGetValue(prefabId, out GameObject prefab))
            {
                Debug.LogError($"[AddressablePrefabPool] '{prefabId}' 주소의 프리팹이 미리 로드되지 않았습니다! Instantiate 실패.");
                return null;
            }

            // 등록된 프리팹의 복제품을 생성하여 반환합니다.
            return Object.Instantiate(prefab, position, rotation);
        }

        /// <summary>
        /// PhotonNetwork.Destroy가 호출될 때 실행되는 함수입니다.
        /// </summary>
        public void Destroy(GameObject gameObject)
        {
            Object.Destroy(gameObject);
        }
    }
}
