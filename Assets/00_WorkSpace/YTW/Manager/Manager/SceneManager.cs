using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YTW
{
    // enum �̸��� �� ��Ȯ�ϰ� �����ϰ�, ���� ���ӿ� �°� Ȯ���� �� �ֵ��� �����մϴ�.
    public enum SceneType
    {
        YTW_TestScene1, 
        YTW_TestScene2, 
        GameScene_Map1,
        GameScene_Map2
    }

    public class SceneManager : Singleton<SceneManager>
    {
        [Header("�ε� UI ������")]
        [Tooltip("Resources ������ �ִ� �ε� UI �������� ���� �Ҵ����ּ���.")]
        [SerializeField] private GameObject _loadingUIPrefab;

        private GameObject _loadingScreenInstance;
        private Slider _progressBarInstance;

        private bool _isLoading = false;
        public bool IsLoading => _isLoading;
        public SceneType CurrentSceneType { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            // �ε� UI�� �ʱ�ȭ�ϴ� �Լ� ȣ��
            InitializeLoadingUI();
        }

        private void InitializeLoadingUI()
        {
            // �������� �Ҵ���� �ʾ����� ��� ����ϰ� ����
            if (_loadingUIPrefab == null)
            {
                Debug.LogError("[SceneManager] Loading UI Prefab�� �Ҵ���� �ʾҽ��ϴ�!");
                return;
            }

            // �̹� �ν��Ͻ��� �ִٸ� �ߺ� ������ ����
            if (_loadingScreenInstance != null) return;

            // 1. �������� ������ ���� ����(Instantiate)�մϴ�.
            _loadingScreenInstance = Instantiate(_loadingUIPrefab);

            // 2. ������ �ε� UI�� �� ��ȯ �� �ı����� �ʵ��� SceneManager�� �ڽ����� ����ϴ�.
            _loadingScreenInstance.transform.SetParent(this.transform);

            // 3. ������ �ε� UI �ν��Ͻ� �ȿ��� Slider ������Ʈ�� ã�ƿɴϴ�.
            _progressBarInstance = _loadingScreenInstance.GetComponentInChildren<Slider>();
            if (_progressBarInstance == null)
            {
                Debug.LogWarning("[SceneManager] �ε� UI ������ �ȿ� Slider ������Ʈ�� �����ϴ�.");
            }

            // 4. �ʱ⿡�� ������ �ʵ��� ��Ȱ��ȭ�մϴ�.
            _loadingScreenInstance.SetActive(false);
        }

        public void LoadScene(SceneType sceneType)
        {
            // ���Ͼ� ���û ����
            if (_isLoading || CurrentSceneType == sceneType)
            {
                return;
            }

            StartCoroutine(IE_LoadSceneAsyncWithProgress(sceneType));
        }

        public void LoadGameScene(SceneType sceneType)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            ShowLoadingScreen(false); // ���α׷��� �ٸ� ����ų� ��Ȱ��ȭ�ϴ� �ɼ�
            PhotonNetwork.LoadLevel(sceneType.ToString());
        }

        public void HideLoadingScreen()
        {
            if (_loadingScreenInstance != null)
            {
                _loadingScreenInstance.SetActive(false);
            }
        }

        public void QuitGame()
        {
            Debug.Log("���� ����");
            Application.Quit();

        #if UNITY_EDITOR
            // �����Ϳ����� �÷��� ��带 ����
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
        }

        private IEnumerator IE_LoadSceneAsyncWithProgress(SceneType sceneType)
        {
            _isLoading = true;
            string sceneName = sceneType.ToString();

            if (Manager.Audio != null)
            {
                Manager.Audio.StopBGM(0.3f);
            }

            ShowLoadingScreen(true);

            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

            while (!asyncLoad.isDone)
            {
                // _progressBarInstance (���� ������ �����̴�)�� ���� ������Ʈ�մϴ�.
                if (_progressBarInstance != null)
                {
                    _progressBarInstance.value = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                }
                yield return null;
            }

            CurrentSceneType = sceneType;

            // ���� �ε�� ���� ���� ���� �ƴ϶�� (GameSceneController�� ���ٸ�)
            // ���⼭ ���� �ε� ȭ���� ���ݴϴ�.
            if (!sceneName.StartsWith("GameScene"))
            {
                HideLoadingScreen();
            }

            _isLoading = false;
        }

        private void ShowLoadingScreen(bool showProgressBar)
        {
            if (_loadingScreenInstance == null)
            {
                return;
            }
            _loadingScreenInstance.SetActive(true);

            if (_progressBarInstance != null)
            {
                _progressBarInstance.gameObject.SetActive(showProgressBar);
                _progressBarInstance.value = 0f;
            }
        }
    }
}
