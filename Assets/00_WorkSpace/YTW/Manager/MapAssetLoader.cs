using UnityEngine;
using Cinemachine; // Ʈ�� ������ �ٷ�� ���� �߰�
using PJW;
using System.Threading.Tasks;
using System.Collections;

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

            var tracks = _spawnedMapInstance.GetComponentsInChildren<CinemachinePathBase>(true);
            if (tracks != null && tracks.Length > 0)
            {
                var reg = TrackRegistry.Instance;
                if (reg != null)
                {
                    reg.RegisterTracks(tracks);
                    Debug.Log($"[MapAssetLoader] TrackRegistry�� {tracks.Length}���� Ʈ���� ���������� ����߽��ϴ�.");
                }
                else
                {
                    Debug.LogWarning("[MapAssetLoader] TrackRegistry�� ���� ����. ����� �����մϴ�.");
                    StartCoroutine(RegisterWhenRegistryReady(tracks));
                }
            }
            else
            {
                Debug.LogWarning($"[MapAssetLoader] ������ �� '{_mapAssetAddress}'���� Ʈ���� ã�� �� �����ϴ�.");
            }
        }

        // ������Ʈ�� �غ�� ������ ��� �� ���
        private IEnumerator RegisterWhenRegistryReady(CinemachinePathBase[] tracks)
        {
            int ticks = 0;
            while (PJW.TrackRegistry.Instance == null && ticks++ < 300)
                yield return null;

            var reg = PJW.TrackRegistry.Instance;
            if (reg != null)
            {
                reg.RegisterTracks(tracks);
                Debug.Log($"[MapAssetLoader] (�������) TrackRegistry�� {tracks.Length}�� ���");
            }
            else
            {
                Debug.LogWarning("[MapAssetLoader] TrackRegistry�� ���� �������� �ʾ� ��� ����");
            }
        }

        // �� ������Ʈ(�� GameObject)�� �ı��� �� ȣ���
        private void OnDestroy()
        {
            // ��ϵ� Ʈ�� ���� �ʱ�ȭ ��û
            if (TrackRegistry.Instance != null)
            {
                TrackRegistry.Instance.ClearTracks();
                Debug.Log("[MapAssetLoader] TrackRegistry�� Ʈ�� ������ �ʱ�ȭ�߽��ϴ�.");
            }

            // �����ߴ� �� �ν��Ͻ��� �޸� ���� ��û
            if (_spawnedMapInstance != null)
            {
                // ResourceManager�� ���� ������ �ν��Ͻ���  ReleaseInstance�� ����
                ResourceManager.Instance?.ReleaseInstance(_spawnedMapInstance);
                _spawnedMapInstance = null;
                Debug.Log($"[MapAssetLoader] '{_mapAssetAddress}' �� �ν��Ͻ��� �����߽��ϴ�.");
            }
        }
    }
}
