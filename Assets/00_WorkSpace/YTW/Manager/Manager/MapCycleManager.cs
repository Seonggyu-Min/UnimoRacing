using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YTW;


namespace YTW
{
    public class MapCycleManager : MonoBehaviour
    {
        public static MapCycleManager Instance { get; private set; }

        [Header("�ε��� �� ���� �ּ� ���")]
        [SerializeField] private string[] _mapAddresses;

        // ���� ���� �ε��ϰ� �ִ� MapAssetLoader�� GameObject
        private GameObject _currentMapLoaderObject;

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
            // ���� ���۵Ǹ� �ٷ� ù ���� �� �ε�
            LoadRandomMap();
        }

        public void LoadRandomMap()
        {
            // 1. ������ �ε�� ���� �ִٸ� �ı�
            //    _currentMapLoaderObject�� �ı��ϸ�, �� �ڽ��� �� �ν��Ͻ���
            //    ������Ʈ�� MapAssetLoader�� OnDestroy()�� �ڵ����� ȣ��Ǿ� ��� ������ ����
            if (_currentMapLoaderObject != null)
            {
                Destroy(_currentMapLoaderObject);
            }

            if (_mapAddresses == null || _mapAddresses.Length == 0)
            {
                Debug.LogError("[MapCycleManager] �ε��� �� �ּ� ����� ����ֽ��ϴ�.");
                return;
            }

            // 2. �� �ּ� ��Ͽ��� �������� �ϳ� ����
            int randomIndex = Random.Range(0, _mapAddresses.Length);
            string randomMapAddress = _mapAddresses[randomIndex];
            Debug.Log($"[MapCycleManager] ���� �� �ε� �õ�: {randomMapAddress}");

            // 3. ���ο� MapAssetLoader�� ���� �� GameObject ����
            _currentMapLoaderObject = new GameObject("MapLoader");

            // 4. MapAssetLoader ������Ʈ �߰� �� ���õ� �ּҷ� �ε� ����
            var mapLoader = _currentMapLoaderObject.AddComponent<MapAssetLoader>();
            _ = mapLoader.InitializeAndLoad(randomMapAddress); // _= : �񵿱� �Լ��� ȣ���ϵ�, ���� ������ ��ٸ��� ����
        }
    }
}
