using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

namespace YTW
{
    // �� �Ϲ��� enum���� ����
    public enum SceneType
    {
        YTW_TestScene1, 
        YTW_TestScene2, 
        Map1
    }

    public class SceneManager : Singleton<SceneManager>
    {
        //[Header("�ε� UI ������")]
        //[Tooltip("Resources ������ �ִ� �ε� UI �������� ���� �Ҵ����ּ���.")]
        //[SerializeField] private GameObject _loadingUIPrefab;

        // Addressables���� �ҷ��� �ε� UI �������� �ּ�.
        // ResourceManager�� �� �ּҸ� ������� �������� �ε��ϰ� Instantiate
        // private const string LOADING_UI_PREFAB_ADDRESS = "LoadingUIPanel";

        // ���� �ν��Ͻ�ȭ�� �ε� UI ������Ʈ��
        private GameObject _loadingScreenInstance;

        // ���� ���� �ε� ������ �÷���. �ߺ� �ε� ����.
        private bool _isLoading = false;
        public bool IsLoading => _isLoading;

        // �ε� UI�� �غ�ƴ��� ����
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;

        // ���� � ���� �ִ��� enum���� ����
        public SceneType CurrentSceneType { get; private set; }

        protected override void Awake()
        {
            base.Awake();
        }

        //private async void Start()
        //{
        //    // ���� ���� �� Loading UI �������� �ҷ����� ���� �񵿱� �ʱ�ȭ ����
        //    await InitializeLoadingUIAsync();
        //}


        //private async Task InitializeLoadingUIAsync()
        //{
        //    Debug.Log("[SceneManager] �ε� UI �ʱ�ȭ ����");
        //    // ResourceManager ���� �ε� UI �������� Addressables���� ������ ���� �ν��Ͻ� ����
        //    _loadingScreenInstance = await ResourceManager.Instance.InstantiateAsync(LOADING_UI_PREFAB_ADDRESS, Vector3.zero, Quaternion.identity);

        //    if (_loadingScreenInstance == null)
        //    {
        //        Debug.LogError("[SceneManager] �ε� UI ������ ������ �����߽��ϴ�!");
        //        return;
        //    }

        //    Debug.Log("[SceneManager] �ε� UI ������ ���� �Ϸ�");
        //    _loadingScreenInstance.transform.SetParent(this.transform);
        //    _loadingScreenInstance.SetActive(false);
        //    _isInitialized = true;
        //}

        // �Ϲ� �� �ε� ( �α��� �� -> �κ� ��)
        public async void LoadScene(SceneType sceneType)
        {
            Debug.Log($"[SceneManager] {sceneType} �ε� ����");
            if (_isLoading || CurrentSceneType == sceneType) return;

            _isLoading = true;
            if (Manager.Audio != null) Manager.Audio.StopBGM(0.3f);

            // ShowLoadingScreen();

            // ResourceManager�� Addressables ��� �� �ε� ���� (enum�� string���� ��ȯ���� address����)
            var sceneInstance = await ResourceManager.Instance.LoadSceneAsync(sceneType.ToString());

            if (sceneInstance.HasValue)
            {
                Debug.Log($"[SceneManager] {sceneType} �ε� ����");
                CurrentSceneType = sceneType;
            }
            else
            {
                Debug.LogError($"[SceneManager] {sceneType} �� �ε忡 �����߽��ϴ�.");
                // HideLoadingScreen();
            }

            _isLoading = false;
        }

        public void LoadGameScene(SceneType sceneType)
        {
            if (!PhotonNetwork.IsMasterClient) return;
             
            PhotonNetwork.LoadLevel(sceneType.ToString());
        }

        // ���� ��� x
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

        // ���� ��� x
        private void ShowLoadingScreen()
        {
            if (_loadingScreenInstance == null)
            {
                Debug.LogError("[SceneManager] �ε� UI �ν��Ͻ��� ����. InitializeLoadingUIAsync() Ȯ�� �ʿ�");
                return;
            }
            Debug.Log("[SceneManager] �ε� UI Ȱ��ȭ");
            _loadingScreenInstance.SetActive(true);
        }
    }
}
