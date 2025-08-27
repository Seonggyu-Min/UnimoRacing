using System;
using UnityEngine;

namespace YTW
{
    /// <summary>
    /// ���� ���� ���۵� ��, �ν����Ϳ� ������ �ּ��� �� ���� ��������
    /// Addressables�� ���� �ε��ϰ� ���� �����ϴ� ���� ��ũ��Ʈ�Դϴ�.
    /// </summary>
    public class MapAssetLoader : MonoBehaviour
    {
        [Header("�ε��� �� ���� ����")]
        [SerializeField] private string _mapAssetAddress; // �ν����Ϳ��� �Է¹��� �ּ�

        // ������ �� ������Ʈ�� ������ ����
        private GameObject _spawnedMapInstance;

        async void Start()
        {
            // �ν����Ϳ� �ּҰ� �ԷµǾ����� Ȯ��
            if (string.IsNullOrWhiteSpace(_mapAssetAddress))
            {
                Debug.LogError("[MapAssetLoader] �ε��� �� ������ �ּҰ� �������� �ʾҽ��ϴ�");
                return;
            }

            if (ResourceManager.Instance == null)
            {
                Debug.LogError("[MapAssetLoader] ResourceManager �ν��Ͻ��� ã�� �� �����ϴ�");
                return;
            }

            try
            {
                // InstantiateAsync ���ο��� EnsureInitializedAsync�� ȣ���ϹǷ� �����ϰ� ���
                var go = await ResourceManager.Instance.InstantiateAsync(_mapAssetAddress, Vector3.zero, Quaternion.identity);

                // �� ��ȯ/������Ʈ �ı� ���� await�� �Ϸ�� �� ����. �� ��� �ε�� ������Ʈ�� �ٷ� ����
                if (this == null || gameObject == null)
                {
                    if (go != null)
                    {
                        Debug.LogWarning("[MapAssetLoader] Start()�� �Ϸ�Ǳ� ���� ������Ʈ�� �ı��Ǿ����ϴ�. ��� �����մϴ�.");
                        ResourceManager.Instance?.ReleaseInstance(go);
                    }
                    return;
                }

                if (go == null)
                {
                    Debug.LogError($"[MapAssetLoader] '{_mapAssetAddress}' �ּ��� ������ �����ϴ� �� �����߽��ϴ�.");
                    return;
                }

                _spawnedMapInstance = go;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MapAssetLoader] InstantiateAsync ����: {_mapAssetAddress} => {ex}");
            }
        }

        private void OnDestroy()
        {
            if (_spawnedMapInstance != null)
            {
                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.ReleaseInstance(_spawnedMapInstance);
                }
                else
                {
                    // ResourceManager�� �̹� �ı�/�� ���¸� �����ϰ� Destroy �õ�
                    Destroy(_spawnedMapInstance);
                }
                _spawnedMapInstance = null;
            }
        }
    }
}
