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

    // 인게임 씬은 적용하지 않습니다.
    public class BgmAutoPlayer : MonoBehaviour
    {
        [SerializeField] private List<SceneBGMBinder> _sceneBgmList;

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void Start()
        {
            StartCoroutine(StartAfterAudioReady());
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }


        private void OnActiveSceneChanged(Scene prev, Scene next)
        {
            ApplyBgmFor(next);
        }

        private void ApplyBgmFor(Scene scene)
        {
            Manager.Audio.StopBGM();

            for (int i = 0; i < _sceneBgmList.Count; i++)
            {
                var bind = _sceneBgmList[i];
                if (scene.name == bind.Scene)
                {
                    Manager.Audio.PlayBGM(bind.BGM);
                    return;
                }
            }

            Debug.LogWarning($"[BgmAutoPlayer] {scene.name} 씬에 BGM이 등록되지 않았습니다.");
        }

        private IEnumerator StartAfterAudioReady()
        {
            if (!Manager.Audio.IsInitialized)
                yield return new WaitUntil(() => Manager.Audio.IsInitialized);

            ApplyBgmFor(SceneManager.GetActiveScene());
        }
    }
}
