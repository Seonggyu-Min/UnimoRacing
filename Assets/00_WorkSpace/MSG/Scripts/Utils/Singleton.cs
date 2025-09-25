using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YSJ.Util;

namespace MSG
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        UnityUtilEx.PrintLog(typeof(T), $"인스턴스 강제 생성 (=> {typeof(T).Name})", LogType.Log, Color.green);
                        GameObject singletonObj = new GameObject(typeof(T).Name);
                        _instance = singletonObj.AddComponent<T>();
                        DontDestroyOnLoad(singletonObj);
                    }
                }
                return _instance;
            }
        }

        protected void SingletonInit()
        {
            if (_instance != null && _instance != this)
            {
                UnityUtilEx.PrintLog(this, $"인스턴스된 오브젝트가 존재해서 해당 타입에서 삭제오브젝트가 삭제됩니다.(=> {typeof(T).Name})", LogType.Log, Color.red);
                Destroy(gameObject);
            }
            else
            {
                _instance = this as T;
                DontDestroyOnLoad(_instance);
                UnityUtilEx.PrintLog(this, $"인스턴스가 존재하지 않아, 인스턴스를 생성 시킵니다.(=> {typeof(T).Name})", LogType.Log, Color.yellow);
            }
        }
    }
}
