using UnityEngine;

namespace YTW
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _isQuitting = false;

        public static T Instance
        {
            get
            {
                // ���ø����̼� ���� ������ �̱����� �ٽ� �����ϴ� ���� ����(������ ����Ŭ ���̴°� ����)
                if (_isQuitting)
                {
                    return null;
                }

                // ������ �������� ���� lock�� ���
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // ������ ���� ã�ƺ��ϴ�.
                        _instance = FindObjectOfType<T>();

                        // ���� ���ٸ� Resources �������� �������� ã�� �����մϴ�.
                        if (_instance == null)
                        {
                            // �������� ��δ� "Managers/[Ŭ�����̸�]"���� �����մϴ�.
                            // ��: "Managers/SceneManager"
                            //string prefabPath = $"Managers/{typeof(T).Name}";
                            //var singletonPrefab = Resources.Load<T>(prefabPath);

                            //if (singletonPrefab != null)
                            //{
                            //    _instance = Instantiate(singletonPrefab);
                            //    _instance.name = $"{typeof(T).Name} (Singleton)";
                            //}

                                // �����յ� ���ٸ� �� ������Ʈ�� ����
                                // Debug.Log($"[Singleton] '{prefabPath}' ��ο� �������� ���� �� ������Ʈ�� �����մϴ�.");
                                var singletonObject = new GameObject();
                                _instance = singletonObject.AddComponent<T>();
                                singletonObject.name = $"{typeof(T).Name} (Singleton)";
                            
                        }
                    }
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            // �̱��� �ν��Ͻ��� �����ϰ�, �ߺ� �ν��Ͻ��� �ı�
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }
    }
}
