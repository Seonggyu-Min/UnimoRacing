using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace YTW
{
    public class PlayerInputHandler : MonoBehaviour
    {
        // �÷��̾� �����տ� Player Input ������Ʈ �߰�, Default Map�� GamePlay
        // PlayerInputHandler�߰�
        // Player Input�� Behavior Invoke Unity Events�� Events���� GamePlay�� �÷��̾� ���� �� OnTocuhInput���
        private PlayerController_Test_YTW _playerController; 

        private void Awake()
        {
            _playerController = GetComponent<PlayerController_Test_YTW>(); 
        }

        public void OnTouchInput(InputAction.CallbackContext context)
        {
            if (!context.started)
            {
                return;
            }

            // ���� EventSystem �־���� 
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("UI ��ġ�� �����Ǿ� �÷��̾� �̵��� ����");
                return;
            }

            if (_playerController == null)
            {
               Debug.LogError("PlayerController�� �Ҵ���� ����");
                return;
            }

            // ��ġ ��ġ
            Vector2 touchPosition = Pointer.current.position.ReadValue();

            // ȭ�� �߾��� �������� ����/�������� �Ǵ�
            if (touchPosition.x < Screen.width / 2)
            {
                // ���� ��ġ �̵�
                _playerController.MoveRight();
            }
            else
            {
                // ������ ��ġ �̵�
                _playerController.MoveLeft();
            }
        }
    }
}