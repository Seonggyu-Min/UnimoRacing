using UnityEngine;
using Cinemachine; // Ʈ�� ������ �ٷ�� ���� �߰�
using PJW;

namespace YTW
{
    public class MapAssetLoader : MonoBehaviour
    {
        [Header("�ε��� �� ���� ����")]
        [SerializeField] private string _mapAssetAddress;

        private GameObject _spawnedMapInstance;

        async void Start()
        {
            if (string.IsNullOrWhiteSpace(_mapAssetAddress))
            {
                Debug.LogError("[MapAssetLoader] �ε��� �� ������ �ּҰ� �������� �ʾҽ��ϴ�.");
                return;
            }

            // �� ������ �񵿱������� ���� �����մϴ�.
            _spawnedMapInstance = await ResourceManager.Instance.InstantiateAsync(_mapAssetAddress, Vector3.zero, Quaternion.identity);

            if (_spawnedMapInstance == null)
            {
                Debug.LogError($"[MapAssetLoader] '{_mapAssetAddress}' �ּ��� ������ �����ϴ� �� �����߽��ϴ�.");
                return;
            }

            // ������ �� ���� �ȿ��� ��� Ʈ��(CinemachinePathBase)�� ã���ϴ�.
            var tracks = _spawnedMapInstance.GetComponentsInChildren<CinemachinePathBase>();
            if (tracks == null || tracks.Length == 0)
            {
                Debug.LogError($"[MapAssetLoader] ������ �� '{_mapAssetAddress}'���� Ʈ���� ã�� �� �����ϴ�.");
                return;
            }

            // ���� �ִ� TrackRegistry�� ã�Ƽ�, ã�� Ʈ������ ����ش޶�� ��û�մϴ�.
            var trackRegistry = FindObjectOfType<TrackRegistry>();
            if (trackRegistry != null)
            {
                trackRegistry.RegisterTracks(tracks);
                Debug.Log($"[MapAssetLoader] TrackRegistry�� {tracks.Length}���� Ʈ���� ���������� ����߽��ϴ�.");
            }
            else
            {
                Debug.LogError("[MapAssetLoader] ������ TrackRegistry�� ã�� �� �����ϴ�!");
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
