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
        YTW_TestScene1, // ���� 
        YTW_TestScene2, // ���� 
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
                // _progressBarInstance (���� ������ �����̴�)�� ���� ������Ʈ�մϴ�.
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
            // _loadingScreenInstance (���� ������ UI)�� Ȱ��ȭ
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
