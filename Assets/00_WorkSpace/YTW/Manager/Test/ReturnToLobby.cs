using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace YTW
{
    public class ReturnToLobby : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            // ESC Ű�� ���ȴ��� Ȯ��
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // �� �Ŵ����� �ε� ���� �ƴ� ���� ����
                if (Manager.Scene != null && !Manager.Scene.IsLoading)
                {
                    Debug.Log("ESC Ű �Է� ����. �κ� ������ ���ư��ϴ�.");

                    // YTW_TestScene1 (�κ� ������ ����)���� �� ��ȯ ��û
                    Manager.Scene.LoadGameScene(SceneType.YTW_TestScene1);
                }
            }
        }
    }
}