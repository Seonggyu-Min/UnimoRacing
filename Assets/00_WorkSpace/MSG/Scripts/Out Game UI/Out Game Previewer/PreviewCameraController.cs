using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    // 기존 카메라를 여러 대 두던 방식에서 하나만 쓰는 방식으로 바꿔서 이제 쓰지 않는 클래스입니다.
    public class PreviewCameraController : MonoBehaviour
    {
        [SerializeField] private Camera _cam;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.7f, -2f);
        [SerializeField] private float _fov = 60f;

        private Transform _target;  // 디버그용으로 밖으로 빼놓음. 없어도 됨
        private RenderTexture _rt;


        private void Awake()
        {
            if (_cam == null)
                if (!TryGetComponent(out _cam))
                    Debug.LogWarning("[PreviewCameraController] Camera 컴포넌트를 찾을 수 없습니다");
        }


        public void Bind(Transform target, RenderTexture rt, int previewLayer)
        {
            _target = target;
            _rt = rt;

            transform.position = target.position + _offset;

            if (_cam != null)
            {
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = new Color(0, 0, 0, 0);
                _cam.fieldOfView = _fov;
                _cam.cullingMask = 1 << previewLayer;
                _cam.targetTexture = _rt;
            }
        }

        public void Unbind()
        {
            _cam.targetTexture = null;
            _target = null;
            _rt = null;
        }
    }
}
