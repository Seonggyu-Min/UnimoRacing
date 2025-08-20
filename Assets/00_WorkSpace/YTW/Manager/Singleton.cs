using UnityEngine;


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
                Debug.LogWarning($"[Singleton] 인스턴스 '{typeof(T)}'는 애플리케이션 종료 시 이미 파괴되어서 다시 생성하지 않습니다.");
                return null;
            }

            // 스레드 안전성을 위해 lock을 사용
            lock (_lock)
            {
                if (_instance == null)
                {
                    // 씬에서 기존 인스턴스를 찾아봅니다.
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        // 씬에 인스턴스가 없으면 새로 생성
                        var singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = $"{typeof(T).Name} (Singleton)";

                        // 씬 전환 시 파괴되지 않도록 설정
                        DontDestroyOnLoad(singletonObject);
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
            Debug.LogWarning($"[Singleton] Instance of '{typeof(T)}' already exists. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}