using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

namespace YTW
{
    // 씬 일므을 enum으로 관리
    public enum SceneType
    {
        YTW_TestScene1, 
        YTW_TestScene2, 
        Map1
    }

    public class SceneManager : Singleton<SceneManager>
    {
        //[Header("로딩 UI 프리팹")]
        //[Tooltip("Resources 폴더에 있는 로딩 UI 프리팹을 직접 할당해주세요.")]
        //[SerializeField] private GameObject _loadingUIPrefab;

        // Addressables에서 불러올 로딩 UI 프리팹의 주소.
        // ResourceManager가 이 주소를 기반으로 프리팹을 로드하고 Instantiate
        // private const string LOADING_UI_PREFAB_ADDRESS = "LoadingUIPanel";

        // 실제 인스턴스화된 로딩 UI 오브젝트ㅁ
        private GameObject _loadingScreenInstance;

        // 현재 씬이 로딩 중인지 플래그. 중복 로딩 방지.
        private bool _isLoading = false;
        public bool IsLoading => _isLoading;

        // 로딩 UI가 준비됐는지 여부
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;

        // 현재 어떤 씬에 있는지 enum으로 추적
        public SceneType CurrentSceneType { get; private set; }

        protected override void Awake()
        {
            base.Awake();
        }

        //private async void Start()
        //{
        //    // 게임 시작 시 Loading UI 프리팹을 불러오기 위한 비동기 초기화 시작
        //    await InitializeLoadingUIAsync();
        //}


        //private async Task InitializeLoadingUIAsync()
        //{
        //    Debug.Log("[SceneManager] 로딩 UI 초기화 시작");
        //    // ResourceManager 통해 로딩 UI 프리팹을 Addressables에서 가져와 씬에 인스턴스 생성
        //    _loadingScreenInstance = await ResourceManager.Instance.InstantiateAsync(LOADING_UI_PREFAB_ADDRESS, Vector3.zero, Quaternion.identity);

        //    if (_loadingScreenInstance == null)
        //    {
        //        Debug.LogError("[SceneManager] 로딩 UI 프리팹 생성에 실패했습니다!");
        //        return;
        //    }

        //    Debug.Log("[SceneManager] 로딩 UI 프리팹 생성 완료");
        //    _loadingScreenInstance.transform.SetParent(this.transform);
        //    _loadingScreenInstance.SetActive(false);
        //    _isInitialized = true;
        //}

        // 일반 씬 로드 ( 로그인 씬 -> 로비 씬)
        public async void LoadScene(SceneType sceneType)
        {
            Debug.Log($"[SceneManager] {sceneType} 로딩 시작");
            if (_isLoading || CurrentSceneType == sceneType) return;

            _isLoading = true;
            if (Manager.Audio != null) Manager.Audio.StopBGM(0.3f);

            // ShowLoadingScreen();

            // ResourceManager의 Addressables 기반 씬 로드 실행 (enum을 string으로 변환으로 address지정)
            var sceneInstance = await ResourceManager.Instance.LoadSceneAsync(sceneType.ToString());

            if (sceneInstance.HasValue)
            {
                Debug.Log($"[SceneManager] {sceneType} 로드 성공");
                CurrentSceneType = sceneType;
            }
            else
            {
                Debug.LogError($"[SceneManager] {sceneType} 씬 로드에 실패했습니다.");
                // HideLoadingScreen();
            }

            _isLoading = false;
        }

        public void LoadGameScene(SceneType sceneType)
        {
            if (!PhotonNetwork.IsMasterClient) return;
             
            PhotonNetwork.LoadLevel(sceneType.ToString());
        }

        // 지금 사용 x
        public void HideLoadingScreen()
        {
            if (_loadingScreenInstance != null)
            {
                _loadingScreenInstance.SetActive(false);
            }
        }

        public void QuitGame()
        {
            Debug.Log("게임 종료");
            Application.Quit();

        #if UNITY_EDITOR
            // 에디터에서는 플레이 모드를 중지
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
        }

        // 지금 사용 x
        private void ShowLoadingScreen()
        {
            if (_loadingScreenInstance == null)
            {
                Debug.LogError("[SceneManager] 로딩 UI 인스턴스가 없음. InitializeLoadingUIAsync() 확인 필요");
                return;
            }
            Debug.Log("[SceneManager] 로딩 UI 활성화");
            _loadingScreenInstance.SetActive(true);
        }
    }
}
