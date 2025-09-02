using UnityEngine;
using Cinemachine; // Ʈ�� ������ �ٷ�� ���� �߰�
using PJW;
using System.Threading.Tasks;

namespace YTW
{
    public class MapAssetLoader : MonoBehaviour
    {
        private string _mapAssetAddress;
        private GameObject _spawnedMapInstance;

        // �ܺ�(MapCycleManager)���� �� �Լ��� ȣ���Ͽ� �ε带 ����
        public async Task InitializeAndLoad(string address)
        {
            _mapAssetAddress = address;

            if (string.IsNullOrWhiteSpace(_mapAssetAddress))
            {
                Debug.LogError("[MapAssetLoader] �ε��� �� ������ �ּҰ� �������� �ʾҽ��ϴ�.");
                return;
            }

            // �� ������ �񵿱������� ���� ����
            _spawnedMapInstance = await ResourceManager.Instance.InstantiateAsync(_mapAssetAddress, Vector3.zero, Quaternion.identity);

            if (_spawnedMapInstance == null)
            {
                Debug.LogError($"[MapAssetLoader] '{_mapAssetAddress}' �ּ��� ������ �����ϴ� �� �����߽��ϴ�.");
                return;
            }

            // ������ �� ������ �θ� �� �δ� ������Ʈ�� �����Ͽ� ������ �����ϰ� ��
            _spawnedMapInstance.transform.SetParent(this.transform);

            // Ʈ�� ã�� �� ���
            var tracks = _spawnedMapInstance.GetComponentsInChildren<CinemachinePathBase>();
            if (tracks != null && tracks.Length > 0)
            {
                /// TrackRegistry.Instance?.RegisterTracks(tracks);
                Debug.Log($"[MapAssetLoader] TrackRegistry�� {tracks.Length}���� Ʈ���� ���������� ����߽��ϴ�.");
            }
            else
            {
                Debug.LogWarning($"[MapAssetLoader] ������ �� '{_mapAssetAddress}'���� Ʈ���� ã�� �� �����ϴ�.");
            }
        }

        // �� ������Ʈ(�� GameObject)�� �ı��� �� ȣ���
        private void OnDestroy()
        {
            // 1. ��ϵ� Ʈ�� ���� �ʱ�ȭ ��û
            if (TrackRegistry.Instance != null)
            {
                /// TrackRegistry.Instance.ClearTracks();
                Debug.Log("[MapAssetLoader] TrackRegistry�� Ʈ�� ������ �ʱ�ȭ�߽��ϴ�.");
            }

            // 2. �����ߴ� �� �ν��Ͻ��� �޸� ���� ��û
            if (_spawnedMapInstance != null)
            {
                // ResourceManager�� ���� ������ �ν��Ͻ��� �ݵ�� ReleaseInstance�� �����ؾ� ��
                ResourceManager.Instance?.ReleaseInstance(_spawnedMapInstance);
                _spawnedMapInstance = null;
                Debug.Log($"[MapAssetLoader] '{_mapAssetAddress}' �� �ν��Ͻ��� �����߽��ϴ�.");
            }
        }
    }
}
