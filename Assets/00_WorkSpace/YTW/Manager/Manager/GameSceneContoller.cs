using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// �� ��ũ��Ʈ�� �� ���� ���� ����ִ� ���� ������Ʈ�� ����
namespace YTW
{
    public class GameSceneContoller : MonoBehaviour
    {
        void Start()
        {
            // �� �ε��� �Ϸ�Ǿ� Start �Լ��� ȣ��Ǹ�,
            // SceneManager���� �ε� ȭ���� ���޶�� ��û
            if (SceneManager.Instance != null)
            {
                SceneManager.Instance.HideLoadingScreen();
            }
        }
    }

}
