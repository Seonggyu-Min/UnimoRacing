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
                // 애플리케이션 종료 시점에 싱글턴을 다시 생성하는 것을 방지(라이프 사이클 꼬이는거 방지)
                if (_isQuitting)
                {
                    return null;
                }

                // 스레드 안전성을 위해 lock을 사용
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // 씬에서 먼저 찾아봅니다.
                        _instance = FindObjectOfType<T>();

                        // 씬에 없다면 Resources 폴더에서 프리팹을 찾아 생성합니다.
                        if (_instance == null)
                        {
                            // 프리팹의 경로는 "Managers/[클래스이름]"으로 가정합니다.
                            // 예: "Managers/SceneManager"
                            //string prefabPath = $"Managers/{typeof(T).Name}";
                            //var singletonPrefab = Resources.Load<T>(prefabPath);

                            //if (singletonPrefab != null)
                            //{
                            //    _instance = Instantiate(singletonPrefab);
                            //    _instance.name = $"{typeof(T).Name} (Singleton)";
                            //}

                                // 프리팹도 없다면 빈 오브젝트를 생성
                                // Debug.Log($"[Singleton] '{prefabPath}' 경로에 프리팹이 없어 빈 오브젝트를 생성합니다.");
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
            // 싱글턴 인스턴스를 설정하고, 중복 인스턴스는 파괴
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
