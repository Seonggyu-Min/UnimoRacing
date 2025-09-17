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
        // 플레이어 프리팹에 Player Input 컴포넌트 추가, Default Map을 GamePlay
        // PlayerInputHandler추가
        // Player Input에 Behavior Invoke Unity Events로 Events에서 GamePlay에 플레이어 연결 후 OnTocuhInput등록
        private PlayerController_Test_YTW _playerController;

        // UI 레이캐스트를 위한 변수들
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

            // 직접 UI를 확인하는 새로운 방식으로 교체
            if (IsPointerOverUI())
            {
                Debug.Log("UI 터치가 감지되어 플레이어 이동을 무시");
                return;
            }

            if (_playerController == null)
            {
                Debug.LogError("PlayerController가 할당되지 않음");
                return;
            }

            // 터치 위치
            Vector2 touchPosition = Pointer.current.position.ReadValue();

            // 화면 중앙을 기준으로 왼쪽/오른쪽을 판단
            if (touchPosition.x < Screen.width / 2)
            {
                // 왼쪽 터치 이동
                _playerController.MoveRight();
            }
            else
            {
                // 오른쪽 터치 이동
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

            // 씬에 있는 모든 GraphicRaycaster에 대해 레이캐스트를 수행합니다.
            EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

            return _raycastResults.Count > 0;
        }
    }
}
