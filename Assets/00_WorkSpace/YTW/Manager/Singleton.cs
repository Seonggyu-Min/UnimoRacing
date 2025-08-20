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
            // ���ø����̼� ���� ������ �̱����� �ٽ� �����ϴ� ���� ����(������ ����Ŭ ���̴°� ����)
            if (_isQuitting)
            {
                Debug.LogWarning($"[Singleton] �ν��Ͻ� '{typeof(T)}'�� ���ø����̼� ���� �� �̹� �ı��Ǿ �ٽ� �������� �ʽ��ϴ�.");
                return null;
            }

            // ������ �������� ���� lock�� ���
            lock (_lock)
            {
                if (_instance == null)
                {
                    // ������ ���� �ν��Ͻ��� ã�ƺ��ϴ�.
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        // ���� �ν��Ͻ��� ������ ���� ����
                        var singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = $"{typeof(T).Name} (Singleton)";

                        // �� ��ȯ �� �ı����� �ʵ��� ����
                        DontDestroyOnLoad(singletonObject);
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
            Debug.LogWarning($"[Singleton] Instance of '{typeof(T)}' already exists. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}