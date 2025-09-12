using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using YTW;
using SceneManager = UnityEngine.SceneManagement.SceneManager;


namespace MSG
{
    [Serializable]
    public class SceneBGMBinder
    {
        [SerializeField, SceneDropdown] private string _scene;
        //[SerializeField] private List<string> _bgmList;
        [SerializeField] private string _bgm;

        public string Scene => _scene;
        public string BGM => _bgm;
    }

    public class BgmAutoPlayer : MonoBehaviour
    {
        [SerializeField] private List<SceneBGMBinder> _sceneBgmList;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private async void Start()
        {
            await Manager.Audio.InitializeAsync();
            ApplyBgmFor(SceneManager.GetActiveScene());
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }



        private void OnActiveSceneChanged(Scene prev, Scene next)
        {
            if (!Manager.Audio.IsInitialized)
            {
                StartCoroutine(ApplyWhenReady(next));              // 초기화 완료되면 적용
                return;
            }
            ApplyBgmFor(next);
        }

        private IEnumerator ApplyWhenReady(Scene scene)
        {
            yield return new WaitUntil(() => Manager.Audio.IsInitialized);
            ApplyBgmFor(scene);
        }


        private void ApplyBgmFor(Scene scene)
        {
            for (int i = 0; i < _sceneBgmList.Count; i++)
            {
                var bind = _sceneBgmList[i];
                if (scene.name == bind.Scene && !string.IsNullOrEmpty(bind.BGM))
                {
                    if (!string.Equals(Manager.Audio.CurrentBgmKey, bind.BGM, StringComparison.OrdinalIgnoreCase))
                    {
                        // 여기서만 정지+교체
                        Manager.Audio.PlayBGM(bind.BGM); 
                    }
                    return;
                }
            }

            Debug.LogWarning($"[BgmAutoPlayer] {scene.name} 씬에 BGM이 등록되지 않았습니다.");
        }
    }
}
