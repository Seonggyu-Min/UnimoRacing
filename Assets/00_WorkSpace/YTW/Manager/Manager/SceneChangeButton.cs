using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YTW;

namespace YTW
{
    public class SceneChangeButton : MonoBehaviour
    {
        [SerializeField] private SceneType targetScene;
        private Button _button;
        // 이 함수를 버튼의 OnClick() 이벤트에 연결

        void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void ChangeScene()
        {
            // Manager를 통해 SceneManager 인스턴스에 접근하고,
            // 인스펙터에서 설정한 targetScene으로 이동하도록 요청
            if (Manager.Scene.IsLoading) return;

            if (_button != null) _button.interactable = false;

            if (Manager.Scene != null)
            {
                // Manager.Scene.LoadScene(targetScene);
                Manager.Scene.LoadGameScene(targetScene);
            }
            else
            {
                Debug.LogError("SceneManager를 찾을 수 없습니다.");
            }
        }

        public void SetInteractable(bool on)
        {
            if (_button != null) _button.interactable = on;
        }
    }

}
