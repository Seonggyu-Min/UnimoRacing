using UnityEngine;
using Cinemachine; // 트랙 정보를 다루기 위해 추가
using PJW;
using System.Threading.Tasks;
using System.Collections;

namespace YTW
{
    public class MapAssetLoader : MonoBehaviour
    {
        private string _mapAssetAddress;
        private GameObject _spawnedMapInstance;

        // 외부(MapCycleManager)에서 이 함수를 호출하여 로드를 시작
        public async Task InitializeAndLoad(string address)
        {
            _mapAssetAddress = address;

            if (string.IsNullOrWhiteSpace(_mapAssetAddress))
            {
                Debug.LogError("[MapAssetLoader] 로드할 맵 에셋의 주소가 지정되지 않았습니다.");
                return;
            }

            // 맵 에셋을 비동기적으로 씬에 생성
            _spawnedMapInstance = await ResourceManager.Instance.InstantiateAsync(_mapAssetAddress, Vector3.zero, Quaternion.identity);

            if (_spawnedMapInstance == null)
            {
                Debug.LogError($"[MapAssetLoader] '{_mapAssetAddress}' 주소의 에셋을 생성하는 데 실패했습니다.");
                return;
            }

            // 생성된 맵 에셋의 부모를 이 로더 오브젝트로 설정하여 관리를 용이하게 함
            _spawnedMapInstance.transform.SetParent(this.transform);

            var tracks = _spawnedMapInstance.GetComponentsInChildren<CinemachinePathBase>(true);
            if (tracks != null && tracks.Length > 0)
            {
                var reg = TrackRegistry.Instance;
                if (reg != null)
                {
                    reg.RegisterTracks(tracks);
                    Debug.Log($"[MapAssetLoader] TrackRegistry에 {tracks.Length}개의 트랙을 성공적으로 등록했습니다.");
                }
                else
                {
                    Debug.LogWarning("[MapAssetLoader] TrackRegistry가 아직 없음. 등록을 지연합니다.");
                    StartCoroutine(RegisterWhenRegistryReady(tracks));
                }
            }
            else
            {
                Debug.LogWarning($"[MapAssetLoader] 생성된 맵 '{_mapAssetAddress}'에서 트랙을 찾을 수 없습니다.");
            }
        }

        // 레지스트리 준비될 때까지 대기 후 등록
        private IEnumerator RegisterWhenRegistryReady(CinemachinePathBase[] tracks)
        {
            int ticks = 0;
            while (PJW.TrackRegistry.Instance == null && ticks++ < 300)
                yield return null;

            var reg = PJW.TrackRegistry.Instance;
            if (reg != null)
            {
                reg.RegisterTracks(tracks);
                Debug.Log($"[MapAssetLoader] (지연등록) TrackRegistry에 {tracks.Length}개 등록");
            }
            else
            {
                Debug.LogWarning("[MapAssetLoader] TrackRegistry가 끝내 생성되지 않아 등록 실패");
            }
        }

        // 이 컴포넌트(와 GameObject)가 파괴될 때 호출됨
        private void OnDestroy()
        {
            // 등록된 트랙 정보 초기화 요청
            if (TrackRegistry.Instance != null)
            {
                TrackRegistry.Instance.ClearTracks();
                Debug.Log("[MapAssetLoader] TrackRegistry의 트랙 정보를 초기화했습니다.");
            }

            // 생성했던 맵 인스턴스의 메모리 해제 요청
            if (_spawnedMapInstance != null)
            {
                // ResourceManager를 통해 생성된 인스턴스는  ReleaseInstance로 해제
                ResourceManager.Instance?.ReleaseInstance(_spawnedMapInstance);
                _spawnedMapInstance = null;
                Debug.Log($"[MapAssetLoader] '{_mapAssetAddress}' 맵 인스턴스를 해제했습니다.");
            }
        }
    }
}
