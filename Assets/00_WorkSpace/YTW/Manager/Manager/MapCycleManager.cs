using System;
using UnityEngine;


namespace YTW
{
    public class MapCycleManager : MonoBehaviour
    {
        public static MapCycleManager Instance { get; private set; }

        [Header("로드할 맵 에셋 주소 목록")]
        [SerializeField] private string[] _mapAddresses;

        // 현재 맵을 로드하고 있는 MapAssetLoader의 GameObject
        private GameObject _currentMapLoaderObject;

        public Action<GameObject> OnLoadRandomMap;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // 씬이 시작되면 바로 첫 랜덤 맵 로드
            LoadRandomMap();
        }

        public void LoadRandomMap()
        {
            // 1. 기존에 로드된 맵이 있다면 파괴
            //    _currentMapLoaderObject를 파괴하면, 그 자식인 맵 인스턴스와
            //    컴포넌트인 MapAssetLoader의 OnDestroy()가 자동으로 호출되어 모든 정리를 수행
            if (_currentMapLoaderObject != null)
            {
                Destroy(_currentMapLoaderObject);
            }

            if (_mapAddresses == null || _mapAddresses.Length == 0)
            {
                Debug.LogError("[MapCycleManager] 로드할 맵 주소 목록이 비어있습니다.");
                return;
            }

            // 2. 맵 주소 목록에서 랜덤으로 하나 선택
            int randomIndex = UnityEngine.Random.Range(0, _mapAddresses.Length);
            string randomMapAddress = _mapAddresses[randomIndex];
            Debug.Log($"[MapCycleManager] 다음 맵 로드 시도: {randomMapAddress}");

            // 3. 새로운 MapAssetLoader를 담을 빈 GameObject 생성
            _currentMapLoaderObject = new GameObject("MapLoader");

            // 4. MapAssetLoader 컴포넌트 추가 및 선택된 주소로 로드 시작
            var mapLoader = _currentMapLoaderObject.AddComponent<MapAssetLoader>();
            _ = mapLoader.InitializeAndLoad(randomMapAddress); // _= : 비동기 함수를 호출하되, 끝날 때까지 기다리지 않음
            OnLoadRandomMap?.Invoke(_currentMapLoaderObject);
        }
    }
}
