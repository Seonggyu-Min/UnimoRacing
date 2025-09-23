using Photon.Pun;
using System;
using UnityEngine;


namespace YTW
{
    public class MapCycleManager : MonoBehaviour
    {
        public static MapCycleManager Instance { get; private set; }
        private bool _isLoading;
        private bool _isLoadedOnce; // 한 번 성공적으로 로드했는지
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 필요하면 유지
        }

        [Header("로드할 맵 에셋 주소 목록")]
        [SerializeField] private string[] _mapAddresses;

        // 현재 맵을 로드하고 있는 MapAssetLoader의 GameObject
        private GameObject _currentMapLoaderObject;

        // 호환 이벤트
        public event Action<GameObject> OnMapLoaderCreated;

        // 외부에서 상태 확인/접근용
        public MapAssetLoader CurrentMapLoader => _currentMapLoaderObject ? _currentMapLoaderObject.GetComponent<MapAssetLoader>() : null;
        public bool HasMapAddresses => _mapAddresses != null && _mapAddresses.Length > 0;

        // 외부에서 투표 결과로 로드 트리거
        public bool LoadFromVote()
        {
            Debug.Log("[MapCycleManager] TryLoadFromVote 호출");
            if (_isLoading)
            {
                Debug.Log("[MapCycleManager] 이미 로딩 중이어서 무시");
                return false;
            }
            if (_isLoadedOnce)
            {
                Debug.Log("[MapCycleManager] 이미 한 번 로드 완료되어 무시");
                return false;
            }

            if (!TryLoadFromVote())
            {
                Debug.LogWarning("[MapCycleManager] 투표 결과 없음/범위 밖/비접속 등으로 로드 안 함");
                return false;
            }
            return true;
        }

        // 방 커스텀 프로퍼티의 투표 인덱스를 읽어 맵을 로드.
        // 성공 true / 실패 false
        private bool TryLoadFromVote()
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            {
                Debug.LogWarning("[MapCycleManager] TryLoadFromVote 실패: Photon 상태(Connected/InRoom/Room) 불가");
                return false;
            }
               

            if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                    PhotonNetworkCustomProperties.KEY_VOTE_WINNER_INDEX, out object raw))
            {
                Debug.LogWarning("[MapCycleManager] TryLoadFromVote 실패: KEY_VOTE_WINNER_INDEX 없음");
                return false;
            }

            Debug.Log($"[MapCycleManager] winner(raw)={raw}");
            if (raw is not int winnerIndex || winnerIndex < 1)
            {
                Debug.LogWarning($"[MapCycleManager] TryLoadFromVote 실패: winnerIndex<1 or not int (raw={raw})");
                return false;
            }
            ;

            if (!HasMapAddresses)
            {
                Debug.LogError("[MapCycleManager] 맵 주소 목록이 비어 있어 투표 결과를 적용할 수 없습니다.");
                return false;
            }

            int addrIndex = winnerIndex - 1;
            Debug.Log($"[MapCycleManager] winnerIndex={winnerIndex} -> addrIndex={addrIndex} / maps={_mapAddresses?.Length}");
            if (addrIndex < 0 || addrIndex >= _mapAddresses.Length)
            {
                Debug.LogWarning($"[MapCycleManager] TryLoadFromVote 실패: addrIndex 범위 밖 (winner={winnerIndex})");
                return false;
            }

            Debug.Log($"[MapCycleManager] 선택 주소='{_mapAddresses[addrIndex]}'");
            LoadMapByIndex(addrIndex);
            return true;
        }

        public void LoadMapByIndex(int index)
        {
            if (!HasMapAddresses) { Debug.LogError("[MapCycleManager] 맵 주소 목록이 비어 있습니다."); return; }
            if (index < 0 || index >= _mapAddresses.Length)
            { Debug.LogError($"[MapCycleManager] 잘못된 맵 인덱스: {index}"); return; }

            InternalLoad(_mapAddresses[index]);
        }

        public void LoadMapByAddress(string mapAddress)
        {
            if (string.IsNullOrWhiteSpace(mapAddress))
            { Debug.LogError("[MapCycleManager] 빈 맵 주소입니다."); return; }

            InternalLoad(mapAddress);
        }

        private void InternalLoad(string address)
        {
            if (_currentMapLoaderObject != null)
                Destroy(_currentMapLoaderObject);

            _currentMapLoaderObject = new GameObject("MapLoader");
            var mapLoader = _currentMapLoaderObject.AddComponent<MapAssetLoader>();

            Debug.Log($"[MapCycleManager] 맵 로드 시작: {address}");
            _isLoading = true;

            _ = mapLoader.InitializeAndLoad(address, onComplete: success =>
            {
                _isLoading = false;
                if (success)
                {
                    _isLoadedOnce = true;
                    Debug.Log("[MapCycleManager] 맵 로드 완료");
                }
                else
                {
                    Debug.LogWarning("[MapCycleManager] 맵 로드 실패");
                }
            });

            // 맵 로더 오브젝트를 이벤트로 즉시 전달
            OnMapLoaderCreated?.Invoke(_currentMapLoaderObject);
        }
    }
}
