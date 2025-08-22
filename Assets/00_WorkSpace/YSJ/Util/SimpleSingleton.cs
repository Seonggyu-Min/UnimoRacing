using UnityEngine;

namespace YSJ.Util
{
    public abstract class SimpleSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        [SerializeField] private bool _isDontDestroyOnLoad = true;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
#if UNITY_EDITOR
                        UnityUtilEx.PrintLog(typeof(T), "인스턴스가 없어 에디터에서 자동 생성됨.", LogType.Warning);
#endif
                        GameObject go = new GameObject($"@{typeof(T)}");
                        _instance = go.AddComponent<T>();

                        if ((_instance as SimpleSingleton<T>).IsDontDestroyOnLoad)
                            DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        protected virtual bool IsDontDestroyOnLoad => _isDontDestroyOnLoad;

        private void Awake() => Init();
        private void OnDestroy() => Destroy();

        protected virtual void Init()
        {
            if (_instance == null)
            {
                _instance = this as T;
                if (IsDontDestroyOnLoad)
                    DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        protected virtual void Destroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
