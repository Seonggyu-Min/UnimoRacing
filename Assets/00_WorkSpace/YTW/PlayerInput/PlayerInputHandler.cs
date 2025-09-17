using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace YTW
{
    public class PlayerInputHandler : MonoBehaviour
    {
        // �÷��̾� �����տ� Player Input ������Ʈ �߰�, Default Map�� GamePlay
        // PlayerInputHandler�߰�
        // Player Input�� Behavior Invoke Unity Events�� Events���� GamePlay�� �÷��̾� ���� �� OnTocuhInput���
        private PlayerController_Test_YTW _playerController;

        // UI ����ĳ��Ʈ�� ���� ������
        private GraphicRaycaster _uiRaycaster;
        private List<RaycastResult> _raycastResults;
        private PointerEventData _pointerEventData;
        private EventSystem _eventSystem;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController_Test_YTW>();
        }

        private void Start()
        {
            _eventSystem = EventSystem.current;
            _raycastResults = new List<RaycastResult>();
        }

        public void OnTouchInput(InputAction.CallbackContext context)
        {

            if (!context.started)
            {
                return;
            }

            // ���� UI�� Ȯ���ϴ� ���ο� ������� ��ü
            if (IsPointerOverUI())
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

        private bool IsPointerOverUI()
        {
            if (_eventSystem == null) return false;

            _pointerEventData = new PointerEventData(_eventSystem)
            {
                position = Pointer.current.position.ReadValue()
            };

            _raycastResults.Clear();

            // ���� �ִ� ��� GraphicRaycaster�� ���� ����ĳ��Ʈ�� �����մϴ�.
            EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

            return _raycastResults.Count > 0;
        }
    }
}
