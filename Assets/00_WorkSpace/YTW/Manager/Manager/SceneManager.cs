using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YTW
{
    // enum 이름을 더 명확하게 변경하고, 실제 게임에 맞게 확장할 수 있도록 수정합니다.
    public enum SceneType
    {
        YTW_TestScene1, // 기존 
        YTW_TestScene2, // 기존 
        GameScene_Map1,
        GameScene_Map2
    }

    public class SceneManager : Singleton<SceneManager>
    {
        [Header("로딩 UI 프리팹")]
        [Tooltip("Resources 폴더에 있는 로딩 UI 프리팹을 직접 할당해주세요.")]
        [SerializeField] private GameObject _loadingUIPrefab;

        private GameObject _loadingScreenInstance;
        private Slider _progressBarInstance;

        private bool _isLoading = false;

        protected override void Awake()
        {
            base.Awake();
            // 로딩 UI를 초기화하는 함수 호출
            InitializeLoadingUI();
        }

        private void InitializeLoadingUI()
        {
            // 프리팹이 할당되지 않았으면 경고를 출력하고 종료
            if (_loadingUIPrefab == null)
            {
                Debug.LogError("[SceneManager] Loading UI Prefab이 할당되지 않았습니다!");
                return;
            }

            // 이미 인스턴스가 있다면 중복 생성을 방지
            if (_loadingScreenInstance != null) return;

            // 1. 프리팹을 실제로 씬에 생성(Instantiate)합니다.
            _loadingScreenInstance = Instantiate(_loadingUIPrefab);

            // 2. 생성된 로딩 UI가 씬 전환 시 파괴되지 않도록 SceneManager의 자식으로 만듭니다.
            _loadingScreenInstance.transform.SetParent(this.transform);

            // 3. 생성된 로딩 UI 인스턴스 안에서 Slider 컴포넌트를 찾아옵니다.
            _progressBarInstance = _loadingScreenInstance.GetComponentInChildren<Slider>();
            if (_progressBarInstance == null)
            {
                Debug.LogWarning("[SceneManager] 로딩 UI 프리팹 안에 Slider 컴포넌트가 없습니다.");
            }

            // 4. 초기에는 보이지 않도록 비활성화합니다.
            _loadingScreenInstance.SetActive(false);
        }

        public void LoadScene(SceneType sceneType)
        {
            if (_isLoading) return;
            StartCoroutine(IE_LoadSceneAsyncWithProgress(sceneType.ToString()));
        }

        public void LoadGameScene(SceneType sceneType)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            ShowLoadingScreen();
            PhotonNetwork.LoadLevel(sceneType.ToString());
        }

        public void HideLoadingScreen()
        {
            if (_loadingScreenInstance != null)
            {
                _loadingScreenInstance.SetActive(false);
            }
        }

        private IEnumerator IE_LoadSceneAsyncWithProgress(string sceneName)
        {
            _isLoading = true;

            if (Manager.Audio != null)
            {
                Manager.Audio.StopBGM(0.3f);
            }

            ShowLoadingScreen();

            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

            while (!asyncLoad.isDone)
            {
                // _progressBarInstance (실제 생성된 슬라이더)의 값을 업데이트합니다.
                if (_progressBarInstance != null)
                {
                    _progressBarInstance.value = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                }
                yield return null;
            }

            _isLoading = false;
        }

        private void ShowLoadingScreen()
        {
            // _loadingScreenInstance (실제 생성된 UI)를 활성화
            if (_loadingScreenInstance != null)
            {
                _loadingScreenInstance.SetActive(true);
            }
            if (_progressBarInstance != null)
            {
                _progressBarInstance.value = 0f;
            }
        }
    }
}
