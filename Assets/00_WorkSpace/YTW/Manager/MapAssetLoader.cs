using System;
using System.Threading.Tasks;
using UnityEngine;
using Cinemachine;         

namespace YTW
{
    // 어드레서블 맵 프리팹을 인스턴스하고, 내부의 CinemachinePathBase 트랙들을 TrackPathRegistry에 등록/해제해주는 로더.
    public class MapAssetLoader : MonoBehaviour
    {
        private string _mapAssetAddress;
        private GameObject _spawnedMapInstance;

        // 현재 인스턴스된 맵 루트
        public GameObject MapInstance => _spawnedMapInstance;

        // 맵이 로드되어 있는지
        public bool IsLoaded => _spawnedMapInstance != null;

        // 맵 로드 완료 이벤트(외부 구독 가능)
        public event Action<GameObject> OnMapLoaded;

        //  맵 언로드 완료 이벤트(외부 구독 가능)
        public event Action OnMapUnloaded;

        private string _currentBgmAddress;
        private AudioSource _bgmSource;


        // 맵 어드레스를 받아 로드 or 인스턴스하고, TrackPathRegistry에 트랙을 등록
        public async Task InitializeAndLoad(
            string address,
            Transform parent = null,
            bool replaceTracks = true,
            Action<bool> onComplete = null)
        {
            bool ok = false;

            // 이미 로드돼 있으면 정리 (TrackPathRegistry는 여기선 건드리지 않고, 맵만 해제)
            if (IsLoaded)
                await UnloadAsync(clearTracks: false);

            _mapAssetAddress = address;

            if (string.IsNullOrWhiteSpace(_mapAssetAddress))
            {
                Debug.LogError("[MapAssetLoader] 로드할 맵 에셋 주소가 비었습니다.");
                onComplete?.Invoke(false);
                return;
            }

            try
            {
                // 1) 맵 인스턴스 (ResourceManager 경로로 통일)
                var inst = await ResourceManager.Instance.InstantiateAsync(
                    _mapAssetAddress, Vector3.zero, Quaternion.identity);

                if (inst == null)
                {
                    Debug.LogError($"[MapAssetLoader] Instantiate 실패: '{_mapAssetAddress}'");
                    onComplete?.Invoke(false);
                    return;
                }

                _spawnedMapInstance = inst;

                // 2) 로딩 끝났는데 이 로더가 이미 파괴됐는지 가드
                if (this == null || gameObject == null)
                {
                    Debug.LogWarning("[MapAssetLoader] 로딩 도중 로더가 파괴됨 → 인스턴스 즉시 해제");
                    ResourceManager.Instance?.ReleaseInstance(_spawnedMapInstance);
                    _spawnedMapInstance = null;
                    onComplete?.Invoke(false);
                    return;
                }

                // 3) 부모 설정
                var targetParent = parent != null ? parent : transform;
                _spawnedMapInstance.transform.SetParent(targetParent, false);

                // 4) 트랙 레지스트리 등록 (필요한 처리만)
                var tpr = TrackPathRegistry.Instance;
                if (tpr != null)
                {
                    // 프로젝트 쪽에서 쓰던 재스캔 호출 유지
                    tpr.RePathLoad();
                    Debug.Log($"[MapAssetLoader] TrackPathRegistry 등록 완료: {tpr.GetPathLength()}개 트랙");
                }
                else
                {
                    Debug.LogWarning("[MapAssetLoader] TrackPathRegistry.Instance == null (등록 생략)");
                }

                // 5) BGM (맵 메타 기반)
                var meta = _spawnedMapInstance.GetComponentInChildren<MapMeta>(true);
                if (meta != null && !string.IsNullOrWhiteSpace(meta.BgmAddress))
                {
                    if (Manager.Audio != null)
                    {
                        try
                        {
                            if (!Manager.Audio.IsInitialized)
                                await Manager.Audio.InitializeAsync();

                            Manager.Audio.StopBGM(); // 임시
                            Manager.Audio.PlayBGM(meta.BgmAddress, fadeTime: 0.5f, forceRestart: false);
                            Debug.Log($"[MapAssetLoader] BGM 재생: {meta.BgmAddress}");
                        }
                        catch (Exception bgmEx)
                        {
                            Debug.LogWarning($"[MapAssetLoader] BGM 재생 중 예외: {bgmEx}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[MapAssetLoader] Manager.Audio 미준비 → BGM 생략");
                    }
                }
                else
                {
                    Debug.Log("[MapAssetLoader] MapMeta 없거나 BGM 주소 비어 있음 → BGM 생략");
                }

                // 6) 이벤트 통지
                OnMapLoaded?.Invoke(_spawnedMapInstance);
                ok = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MapAssetLoader] InitializeAndLoad 예외: {ex}");
            }
            finally
            {
                onComplete?.Invoke(ok);
            }
        }
        // 맵 언로드 + 트랙 레지스트리 초기화
        public async Task UnloadAsync(bool clearTracks = true)
        {
            try
            {
                //if (clearTracks && TrackPathRegistry.Instance != null)
                //{
                //    TrackPathRegistry.Instance.ClearTracks();
                //    Debug.Log("[MapAssetLoader] TrackPathRegistry 트랙 정보 초기화");
                //}

                if (_spawnedMapInstance != null)
                {
                    ResourceManager.Instance?.ReleaseInstance(_spawnedMapInstance);
                    Debug.Log($"[MapAssetLoader] '{_mapAssetAddress}' 맵 인스턴스 해제");
                    _spawnedMapInstance = null;
                }

                OnMapUnloaded?.Invoke();

                // Addressables/리소스 GC 틱 양보
                await Task.Yield();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MapAssetLoader] UnloadAsync 예외: {ex}");
            }
        }

        private void OnDestroy()
        {
            // OnDestroy는 await 불가: 최선의 정리만 수행
            //if (TrackPathRegistry.Instance != null)
            //{
            //    TrackPathRegistry.Instance.ClearTracks();
            //    Debug.Log("[MapAssetLoader] (OnDestroy) TrackPathRegistry 초기화");
            //}

            if (_spawnedMapInstance != null)
            {
                ResourceManager.Instance?.ReleaseInstance(_spawnedMapInstance);
                Debug.Log($"[MapAssetLoader] (OnDestroy) '{_mapAssetAddress}' 맵 인스턴스 해제");
                _spawnedMapInstance = null;
            }
        }
    }
}
