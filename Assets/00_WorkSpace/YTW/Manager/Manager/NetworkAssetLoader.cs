using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Photon.Pun;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace YTW
{
    /// <summary>
    /// 네트워크 프리팹을 Addressables를 통해 미리 로드하고 Photon의 PrefabPool을 설정하는 클래스
    /// </summary>
    public class NetworkAssetLoader
    {
        private static NetworkAssetLoader _instance;
        public static NetworkAssetLoader Instance => _instance ?? (_instance = new NetworkAssetLoader());
        private NetworkAssetLoader() { }

        public AddressablePrefabPool PrefabPool { get; private set; }
        public bool IsReady { get; private set; } = false;
        public event Action OnAssetsReady;

        private bool _isInitializing = false;
        // 해제를 위해 로드 핸들을 직접 저장합니다.
        private AsyncOperationHandle<IList<GameObject>> _loadHandle;

        /// <summary>
        /// 지정된 라벨을 가진 모든 네트워크 프리팹들을 로드하고 Photon Prefab Pool을 초기화합니다.
        /// </summary>
        public async Task InitializeAndPreloadAsync(string label)
        {
            if (IsReady || _isInitializing) return;

            _isInitializing = true;
            Debug.Log($"[NetworkAssetLoader] '{label}' 라벨을 가진 프리팹 사전 로드를 시작.");

            // 커스텀 풀 생성 및 포톤에 등록
            PrefabPool = new AddressablePrefabPool();
            PhotonNetwork.PrefabPool = PrefabPool;

            // 라벨을 사용하여 프리팹 비동기 로드 및 등록
            await PreloadNetworkPrefabsByLabel(label);

            // 준비 완료 상태로 전환하고 이벤트 호출
            IsReady = true;
            _isInitializing = false;
            OnAssetsReady?.Invoke();
            Debug.Log("[NetworkAssetLoader] 모든 네트워크 프리팹 준비 완료.");
        }

        private async Task PreloadNetworkPrefabsByLabel(string label)
        {
            // 라벨을 사용하여 해당 에셋들의 위치(주소 정보)를 먼저 찾습니다.
            var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
            IList<IResourceLocation> locations = await locationsHandle.Task;

            if (locations == null || locations.Count == 0)
            {
                Debug.LogWarning($"[NetworkAssetLoader] '{label}' 라벨을 가진 에셋을 찾을 수 없습니다.");
                Addressables.Release(locationsHandle);
                return;
            }

            // 찾은 위치들을 기반으로 실제 GameObject들을 한 번에 로드
            _loadHandle = Addressables.LoadAssetsAsync<GameObject>(locations, null);
            IList<GameObject> loadedPrefabs = await _loadHandle.Task;

            // 로드된 프리팹과 위치 정보를 매칭하여 실제 주소로 풀에 등록
            if (loadedPrefabs != null)
            {
                for (int i = 0; i < loadedPrefabs.Count; i++)
                {
                    var prefab = loadedPrefabs[i];
                    var location = locations[i];
                    if (prefab != null && location != null)
                    {
                        // prefab.name 대신, 실제 어드레서블 주소인 location.PrimaryKey를 사용
                        string address = location.PrimaryKey;
                        PrefabPool.RegisterPrefab(address, prefab);
                    }
                }
            }

            // 위치 정보를 담고 있던 핸들은 더 이상 필요 없으므로 해제
            Addressables.Release(locationsHandle);
        }

        /// <summary>
        /// 미리 로드했던 모든 네트워크 프리팹의 참조를 해제합니다.
        /// </summary>
        public void ReleasePreloadedAssets()
        {
            // Addressables 로드 핸들 해제
            if (_loadHandle.IsValid())
            {
                Debug.Log("[NetworkAssetLoader] 미리 로드했던 에셋들을 해제합니다.");
                Addressables.Release(_loadHandle);
            }

            // PrefabPool 정리: 풀 내부 캐시 비우기
            try
            {
                PrefabPool?.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkAssetLoader] PrefabPool.Clear 중 경고: {ex}");
            }

            // PhotonNetwork.PrefabPool 원복
            // 현재 풀을 우리가 세팅했던 풀로 사용중인 경우에만 해제
            try
            {
                if (PhotonNetwork.PrefabPool == PrefabPool)
                {
                    PhotonNetwork.PrefabPool = null; // 또는 기본 풀 인스턴스가 있다면 그걸로 교체
                    Debug.Log("[NetworkAssetLoader] PhotonNetwork.PrefabPool 해제 완료.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkAssetLoader] PrefabPool 원복 중 경고: {ex}");
            }

            // 4) 내부 상태 초기화
            PrefabPool = null;
            IsReady = false;
            _instance = null;
        }
    }
}
