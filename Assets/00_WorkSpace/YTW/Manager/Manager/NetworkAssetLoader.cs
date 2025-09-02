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
    /// ��Ʈ��ũ �������� Addressables�� ���� �̸� �ε��ϰ� Photon�� PrefabPool�� �����ϴ� Ŭ����
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
        // ������ ���� �ε� �ڵ��� ���� �����մϴ�.
        private AsyncOperationHandle<IList<GameObject>> _loadHandle;

        /// <summary>
        /// ������ ���� ���� ��� ��Ʈ��ũ �����յ��� �ε��ϰ� Photon Prefab Pool�� �ʱ�ȭ�մϴ�.
        /// </summary>
        public async Task InitializeAndPreloadAsync(string label)
        {
            if (IsReady || _isInitializing) return;

            _isInitializing = true;
            Debug.Log($"[NetworkAssetLoader] '{label}' ���� ���� ������ ���� �ε带 ����.");

            // 1. Ŀ���� Ǯ ���� �� ���濡 ���
            PrefabPool = new AddressablePrefabPool();
            PhotonNetwork.PrefabPool = PrefabPool;

            // 2. ���� ����Ͽ� ������ �񵿱� �ε� �� ���
            await PreloadNetworkPrefabsByLabel(label);

            // 3. �غ� �Ϸ� ���·� ��ȯ�ϰ� �̺�Ʈ ȣ��
            IsReady = true;
            _isInitializing = false;
            OnAssetsReady?.Invoke();
            Debug.Log("[NetworkAssetLoader] ��� ��Ʈ��ũ ������ �غ� �Ϸ�.");
        }

        private async Task PreloadNetworkPrefabsByLabel(string label)
        {
            // 1. ���� ����Ͽ� �ش� ���µ��� ��ġ(�ּ� ����)�� ���� ã���ϴ�.
            var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
            IList<IResourceLocation> locations = await locationsHandle.Task;

            if (locations == null || locations.Count == 0)
            {
                Debug.LogWarning($"[NetworkAssetLoader] '{label}' ���� ���� ������ ã�� �� �����ϴ�.");
                Addressables.Release(locationsHandle);
                return;
            }

            // 2. ã�� ��ġ���� ������� ���� GameObject���� �� ���� �ε��մϴ�.
            _loadHandle = Addressables.LoadAssetsAsync<GameObject>(locations, null);
            IList<GameObject> loadedPrefabs = await _loadHandle.Task;

            // 3. �ε�� �����հ� ��ġ ������ ��Ī�Ͽ� ���� �ּҷ� Ǯ�� ����մϴ�.
            if (loadedPrefabs != null)
            {
                for (int i = 0; i < loadedPrefabs.Count; i++)
                {
                    var prefab = loadedPrefabs[i];
                    var location = locations[i];
                    if (prefab != null && location != null)
                    {
                        // prefab.name ���, ���� ��巹���� �ּ��� location.PrimaryKey�� ����մϴ�.
                        string address = location.PrimaryKey;
                        PrefabPool.RegisterPrefab(address, prefab);
                    }
                }
            }

            // ��ġ ������ ��� �ִ� �ڵ��� �� �̻� �ʿ� �����Ƿ� �����մϴ�.
            Addressables.Release(locationsHandle);
        }

        /// <summary>
        /// �̸� �ε��ߴ� ��� ��Ʈ��ũ �������� ������ �����մϴ�.
        /// </summary>
        public void ReleasePreloadedAssets()
        {
            if (!_loadHandle.IsValid()) return;

            Debug.Log("[NetworkAssetLoader] �̸� �ε��ߴ� ���µ��� �����մϴ�.");
            Addressables.Release(_loadHandle);

            IsReady = false;
            _instance = null;
        }
    }
}
