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
        public async Task InitializeAndLoad(string address, Transform parent = null, bool replaceTracks = true)
        {
            // 이미 로드돼 있으면 정리
            if (IsLoaded)
                await UnloadAsync(clearTracks: false); // 곧 새 맵으로 갈 거면 중복 클리어는 생략

            _mapAssetAddress = address;

            if (string.IsNullOrWhiteSpace(_mapAssetAddress))
            {
                Debug.LogError("[MapAssetLoader] 로드할 맵 에셋의 주소가 지정되지 않았습니다.");
                return;
            }

            try
            {
                // 맵 인스턴스
                _spawnedMapInstance = await ResourceManager.Instance.InstantiateAsync(
                    _mapAssetAddress, Vector3.zero, Quaternion.identity);

                if (_spawnedMapInstance == null)
                {
                    Debug.LogError($"[MapAssetLoader] '{_mapAssetAddress}' 주소의 에셋을 생성하는 데 실패했습니다.");
                    return;
                }

                // 부모 설정
                var targetParent = parent != null ? parent : this.transform;
                _spawnedMapInstance.transform.SetParent(targetParent, worldPositionStays: false);

                // 트랙 레지스트리 등록
                var tpr = TrackPathRegistry.Instance;
                if (tpr != null)
                {
                    // 맵 루트 기준으로 CinemechinePathBase 전부 찾아 등록
                    //tpr.RegisterFromRoot(_spawnedMapInstance.transform, replace: replaceTracks);
                    tpr.RePathLoad();
                    Debug.Log($"[MapAssetLoader] TrackPathRegistry 등록 완료: {tpr.GetPathLength()}개 트랙");
                }
                else
                {
                    Debug.LogWarning("[MapAssetLoader] TrackPathRegistry.Instance 가 null 입니다. (등록 생략)");
                }


                OnMapLoaded?.Invoke(_spawnedMapInstance);

                var meta = _spawnedMapInstance.GetComponentInChildren<MapMeta>(true);
                if (meta != null && !string.IsNullOrWhiteSpace(meta.BgmAddress))
                {
                    if (Manager.Audio != null)
                    {
                        try
                        {
                            // AudioManager가 아직 초기화 전이면 보장
                            if (!Manager.Audio.IsInitialized)
                            {
                                await Manager.Audio.InitializeAsync();
                            }
                            Manager.Audio.StopBGM(); // 임시
                            // 핵심: 주소 문자열을 그대로 PlayBGM에 전달 (ClipName과 동일해야 함)
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
                        Debug.LogWarning("[MapAssetLoader] Manager.Audio 가 준비되지 않아 BGM 재생을 건너뜁니다.");
                    }
                }
                else
                {
                    Debug.Log("[MapAssetLoader] MapMeta가 없거나 BGM 주소가 비어 있어 재생을 생략합니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MapAssetLoader] InitializeAndLoad 예외: {ex}");
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
