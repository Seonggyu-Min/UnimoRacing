using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YTW;

namespace YTW
{
    public class SceneChangeButton : MonoBehaviour
    {
        [SerializeField] private SceneType targetScene;

        // �� �Լ��� ��ư�� OnClick() �̺�Ʈ�� ����
        public void ChangeScene()
        {
            // Manager�� ���� SceneManager �ν��Ͻ��� �����ϰ�,
            // �ν����Ϳ��� ������ targetScene���� �̵��ϵ��� ��û
            if (Manager.Scene != null)
            {
                Manager.Scene.LoadScene(targetScene);
            }
            else
            {
                Debug.LogError("SceneManager�� ã�� �� �����ϴ�! Manager �������� ���� �ִ��� Ȯ���ϼ���.");
            }
        }
    }

}
