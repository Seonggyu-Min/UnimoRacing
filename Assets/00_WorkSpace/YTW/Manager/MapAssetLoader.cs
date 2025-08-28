using UnityEngine;
using Cinemachine; // 트랙 정보를 다루기 위해 추가
using PJW;

namespace YTW
{
    public class MapAssetLoader : MonoBehaviour
    {
        [Header("로드할 맵 에셋 정보")]
        [SerializeField] private string _mapAssetAddress;

        private GameObject _spawnedMapInstance;

        async void Start()
        {
            if (string.IsNullOrWhiteSpace(_mapAssetAddress))
            {
                Debug.LogError("[MapAssetLoader] 로드할 맵 에셋의 주소가 지정되지 않았습니다.");
                return;
            }

            // 맵 에셋을 비동기적으로 씬에 생성합니다.
            _spawnedMapInstance = await ResourceManager.Instance.InstantiateAsync(_mapAssetAddress, Vector3.zero, Quaternion.identity);

            if (_spawnedMapInstance == null)
            {
                Debug.LogError($"[MapAssetLoader] '{_mapAssetAddress}' 주소의 에셋을 생성하는 데 실패했습니다.");
                return;
            }

            // 생성된 맵 에셋 안에서 모든 트랙(CinemachinePathBase)을 찾습니다.
            var tracks = _spawnedMapInstance.GetComponentsInChildren<CinemachinePathBase>();
            if (tracks == null || tracks.Length == 0)
            {
                Debug.LogError($"[MapAssetLoader] 생성된 맵 '{_mapAssetAddress}'에서 트랙을 찾을 수 없습니다.");
                return;
            }

            // 씬에 있는 TrackRegistry를 찾아서, 찾은 트랙들을 등록해달라고 요청합니다.
            var trackRegistry = FindObjectOfType<TrackRegistry>();
            if (trackRegistry != null)
            {
                trackRegistry.RegisterTracks(tracks);
                Debug.Log($"[MapAssetLoader] TrackRegistry에 {tracks.Length}개의 트랙을 성공적으로 등록했습니다.");
            }
            else
            {
                Debug.LogError("[MapAssetLoader] 씬에서 TrackRegistry를 찾을 수 없습니다!");
            }
        }

        private void OnDestroy()
        {
            if (_spawnedMapInstance != null)
            {
                ResourceManager.Instance?.ReleaseInstance(_spawnedMapInstance);
            }
        }
    }
}
