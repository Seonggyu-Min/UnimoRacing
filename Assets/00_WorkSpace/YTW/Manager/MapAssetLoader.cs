using System;
using System.Threading.Tasks;
using UnityEngine;
using Cinemachine;         

namespace YTW
{
    // ��巹���� �� �������� �ν��Ͻ��ϰ�, ������ CinemachinePathBase Ʈ������ TrackPathRegistry�� ���/�������ִ� �δ�.
    public class MapAssetLoader : MonoBehaviour
    {
        private string _mapAssetAddress;
        private GameObject _spawnedMapInstance;

        // ���� �ν��Ͻ��� �� ��Ʈ
        public GameObject MapInstance => _spawnedMapInstance;

        // ���� �ε�Ǿ� �ִ���
        public bool IsLoaded => _spawnedMapInstance != null;

        // �� �ε� �Ϸ� �̺�Ʈ(�ܺ� ���� ����)
        public event Action<GameObject> OnMapLoaded;

        //  �� ��ε� �Ϸ� �̺�Ʈ(�ܺ� ���� ����)
        public event Action OnMapUnloaded;

        private string _currentBgmAddress;
        private AudioSource _bgmSource;


        // �� ��巹���� �޾� �ε� or �ν��Ͻ��ϰ�, TrackPathRegistry�� Ʈ���� ���
        public async Task InitializeAndLoad(string address, Transform parent = null, bool replaceTracks = true)
        {
            // �̹� �ε�� ������ ����
            if (IsLoaded)
                await UnloadAsync(clearTracks: false); // �� �� ������ �� �Ÿ� �ߺ� Ŭ����� ����

            _mapAssetAddress = address;

            if (string.IsNullOrWhiteSpace(_mapAssetAddress))
            {
                Debug.LogError("[MapAssetLoader] �ε��� �� ������ �ּҰ� �������� �ʾҽ��ϴ�.");
                return;
            }

            try
            {
                // �� �ν��Ͻ�
                _spawnedMapInstance = await ResourceManager.Instance.InstantiateAsync(
                    _mapAssetAddress, Vector3.zero, Quaternion.identity);

                if (_spawnedMapInstance == null)
                {
                    Debug.LogError($"[MapAssetLoader] '{_mapAssetAddress}' �ּ��� ������ �����ϴ� �� �����߽��ϴ�.");
                    return;
                }

                // �θ� ����
                var targetParent = parent != null ? parent : this.transform;
                _spawnedMapInstance.transform.SetParent(targetParent, worldPositionStays: false);

                // Ʈ�� ������Ʈ�� ���
                var tpr = TrackPathRegistry.Instance;
                if (tpr != null)
                {
                    // �� ��Ʈ �������� CinemechinePathBase ���� ã�� ���
                    tpr.RegisterFromRoot(_spawnedMapInstance.transform, replace: replaceTracks);
                    Debug.Log($"[MapAssetLoader] TrackPathRegistry ��� �Ϸ�: {tpr.GetPathLength()}�� Ʈ��");
                }
                else
                {
                    Debug.LogWarning("[MapAssetLoader] TrackPathRegistry.Instance �� null �Դϴ�. (��� ����)");
                }

                OnMapLoaded?.Invoke(_spawnedMapInstance);

                var meta = _spawnedMapInstance.GetComponentInChildren<MapMeta>(true);
                if (meta != null && !string.IsNullOrWhiteSpace(meta.BgmAddress))
                {
                    if (Manager.Audio != null)
                    {
                        try
                        {
                            // AudioManager�� ���� �ʱ�ȭ ���̸� ����
                            if (!Manager.Audio.IsInitialized)
                            {
                                await Manager.Audio.InitializeAsync();
                            }
                            Manager.Audio.StopBGM(); // �ӽ�
                            // �ٽ�: �ּ� ���ڿ��� �״�� PlayBGM�� ���� (ClipName�� �����ؾ� ��)
                            Manager.Audio.PlayBGM(meta.BgmAddress, fadeTime: 0.5f, forceRestart: false);
                            Debug.Log($"[MapAssetLoader] BGM ���: {meta.BgmAddress}");
                        }
                        catch (Exception bgmEx)
                        {
                            Debug.LogWarning($"[MapAssetLoader] BGM ��� �� ����: {bgmEx}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[MapAssetLoader] Manager.Audio �� �غ���� �ʾ� BGM ����� �ǳʶݴϴ�.");
                    }
                }
                else
                {
                    Debug.Log("[MapAssetLoader] MapMeta�� ���ų� BGM �ּҰ� ��� �־� ����� �����մϴ�.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MapAssetLoader] InitializeAndLoad ����: {ex}");
            }
        }

        // �� ��ε� + Ʈ�� ������Ʈ�� �ʱ�ȭ
        public async Task UnloadAsync(bool clearTracks = true)
        {
            try
            {
                if (clearTracks && TrackPathRegistry.Instance != null)
                {
                    TrackPathRegistry.Instance.ClearTracks();
                    Debug.Log("[MapAssetLoader] TrackPathRegistry Ʈ�� ���� �ʱ�ȭ");
                }

                if (_spawnedMapInstance != null)
                {
                    ResourceManager.Instance?.ReleaseInstance(_spawnedMapInstance);
                    Debug.Log($"[MapAssetLoader] '{_mapAssetAddress}' �� �ν��Ͻ� ����");
                    _spawnedMapInstance = null;
                }

                OnMapUnloaded?.Invoke();

                // Addressables/���ҽ� GC ƽ �纸
                await Task.Yield();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MapAssetLoader] UnloadAsync ����: {ex}");
            }
        }

        private void OnDestroy()
        {
            // OnDestroy�� await �Ұ�: �ּ��� ������ ����
            if (TrackPathRegistry.Instance != null)
            {
                TrackPathRegistry.Instance.ClearTracks();
                Debug.Log("[MapAssetLoader] (OnDestroy) TrackPathRegistry �ʱ�ȭ");
            }

            if (_spawnedMapInstance != null)
            {
                ResourceManager.Instance?.ReleaseInstance(_spawnedMapInstance);
                Debug.Log($"[MapAssetLoader] (OnDestroy) '{_mapAssetAddress}' �� �ν��Ͻ� ����");
                _spawnedMapInstance = null;
            }
        }
    }
}
