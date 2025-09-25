using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using UnityEngine;
using YSJ.Util;


namespace MSG
{
    // 현재 구조는 프로퍼티들이 초기화되지 않은 상태로 접근할 수 있음에 주의해야 됩니다.
    public class FirebaseManager : Singleton<FirebaseManager>
    {
        #region Fields And Properties

        private FirebaseApp _app;
        private FirebaseAuth _auth;
        private FirebaseDatabase _database;
        public bool IsReady => _app != null && _auth != null && _database != null;

        public event Action OnFirebaseReady;

        public FirebaseApp App
        {
            get
            {
                if (_app == null)
                {
                    _app = FirebaseApp.DefaultInstance;
                }
                return _app;
            }
        }

        public FirebaseAuth Auth
        {
            get
            {
                if (_auth == null)
                {
                    _auth = FirebaseAuth.DefaultInstance;
                }
                return _auth;
            }
        }

        public FirebaseDatabase Database
        {
            get
            {
                if (_database == null)
                {
                    _database = FirebaseDatabase.DefaultInstance;
                }
                return _database;
            }
        }

        #endregion

        private void Awake()
        {
            this.PrintLog("파이어 베이스 Awake() 진행");
            SingletonInit();
            this.PrintLog("파이어 베이스 Awake() 진행 완료");
        }

        private void OnDestroy()
        {
            this.PrintLog("파이어 베이스 OnDestroy() 실행");
        }

        private void Start()
        {
            this.PrintLog("파이어 베이스 Start() 진행");
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                Firebase.DependencyStatus dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    this.PrintLog("파이어 베이스 설정이 모두 충족되어 사용할 수 있는 상황");
                    _app = FirebaseApp.DefaultInstance;
                    _auth = FirebaseAuth.DefaultInstance;
                    _database = FirebaseDatabase.DefaultInstance;

                    _database.GoOnline();

                    OnFirebaseReady?.Invoke();
                }
                else
                {
                    this.PrintLog($"파이어 베이스 설정이 충족되지 않아 실패했습니다. 이유: {dependencyStatus}", LogType.Error);
                    _app = null;
                    _auth = null;
                    _database = null;
                }
            });
            this.PrintLog("파이어 베이스 Start() 진행 완료");
        }

        private void OnApplicationQuit()
        {
            if (Database != null) Database.GoOffline();
        }
        private void PrintLog(string log, LogType type = LogType.Log)
        {
            this.PrintLog(log, type, Color.cyan);
        }
    }
}
