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
        // �� �Լ��� ��ư�� OnClick() �̺�Ʈ�� ����

        void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void ChangeScene()
        {
            // Manager�� ���� SceneManager �ν��Ͻ��� �����ϰ�,
            // �ν����Ϳ��� ������ targetScene���� �̵��ϵ��� ��û
            if (Manager.Scene.IsLoading) return;

            if (_button != null) _button.interactable = false;

            if (Manager.Scene != null)
            {
                Manager.Scene.LoadScene(targetScene);
            }
            else
            {
                Debug.LogError("SceneManager�� ã�� �� �����ϴ�! Manager �������� ���� �ִ��� Ȯ���ϼ���.");
            }
        }

        public void SetInteractable(bool on)
        {
            if (_button != null) _button.interactable = on;
        }
    }

}
