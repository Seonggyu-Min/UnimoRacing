using System;
using UnityEngine;

namespace YTW
{
    /// <summary>
    /// 게임 씬이 시작될 때, 인스펙터에 지정된 주소의 맵 에셋 프리팹을
    /// Addressables를 통해 로드하고 씬에 생성하는 범용 스크립트입니다.
    /// </summary>
    public class MapAssetLoader : MonoBehaviour
    {
        [Header("로드할 맵 에셋 정보")]
        [SerializeField] private string _mapAssetAddress; // 인스펙터에서 입력받을 주소

        // 생성된 맵 오브젝트를 저장할 변수
        private GameObject _spawnedMapInstance;

        async void Start()
        {
            // 인스펙터에 주소가 입력되었는지 확인
            if (string.IsNullOrWhiteSpace(_mapAssetAddress))
            {
                Debug.LogError("[MapAssetLoader] 로드할 맵 에셋의 주소가 지정되지 않았습니다");
                return;
            }

            if (ResourceManager.Instance == null)
            {
                Debug.LogError("[MapAssetLoader] ResourceManager 인스턴스를 찾을 수 없습니다");
                return;
            }

            try
            {
                // InstantiateAsync 내부에서 EnsureInitializedAsync를 호출하므로 안전하게 대기
                var go = await ResourceManager.Instance.InstantiateAsync(_mapAssetAddress, Vector3.zero, Quaternion.identity);

                // 씬 전환/오브젝트 파괴 도중 await가 완료될 수 있음. 이 경우 로드된 오브젝트를 바로 해제
                if (this == null || gameObject == null)
                {
                    if (go != null)
                    {
                        Debug.LogWarning("[MapAssetLoader] Start()가 완료되기 전에 오브젝트가 파괴되었습니다. 즉시 해제합니다.");
                        ResourceManager.Instance?.ReleaseInstance(go);
                    }
                    return;
                }

                if (go == null)
                {
                    Debug.LogError($"[MapAssetLoader] '{_mapAssetAddress}' 주소의 에셋을 생성하는 데 실패했습니다.");
                    return;
                }

                _spawnedMapInstance = go;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MapAssetLoader] InstantiateAsync 예외: {_mapAssetAddress} => {ex}");
            }
        }

        private void OnDestroy()
        {
            if (_spawnedMapInstance != null)
            {
                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.ReleaseInstance(_spawnedMapInstance);
                }
                else
                {
                    // ResourceManager가 이미 파괴/널 상태면 안전하게 Destroy 시도
                    Destroy(_spawnedMapInstance);
                }
                _spawnedMapInstance = null;
            }
        }
    }
}
