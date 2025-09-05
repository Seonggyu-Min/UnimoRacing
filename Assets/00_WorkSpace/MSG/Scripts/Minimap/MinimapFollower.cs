using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class MinimapFollower : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Camera _minimapCamera;
        [SerializeField] private Transform _player;

        [Header("Pose")]
        [SerializeField, Range(10f, 89f)] private float _pitchDeg = 55f; // 카트라이더처럼 살짝 눕히는 각도
        [SerializeField] private float _height = 60f;                    // 위로 띄우는 높이
        [SerializeField] private float _backOffset = 40f;                // 진행 반대(-forward)로 떨어질 거리
        [SerializeField] private float _lookAhead = 10f;                 // 시선은 플레이어 앞쪽을 보게

        [Header("Behavior")]
        [SerializeField] private bool _rotateWithPlayer = true;          // true면 맵이 플레이어 방향으로 회전
        [SerializeField] private float _posSmooth = 12f;                 // 위치 스무딩
        [SerializeField] private float _rotSmooth = 12f;                 // 회전 스무딩
        [SerializeField] private float _orthoSize = 80f;                 // 오소그래픽 사이즈(줌)

        private bool _isReady;

        private void Start()
        {
            if (_minimapCamera != null)
            {
                _minimapCamera.orthographic = true;
                _minimapCamera.orthographicSize = _orthoSize;
            }

            // TODO: 준비 신호 받는 로직 추가
            //if (_player != null) OnReady();
        }

        private void LateUpdate()
        {
            if (/*!_isReady || */_minimapCamera == null || _player == null) return;
            Follow();
        }

        //public void OnReady()
        //{
        //    _isReady = true;
        //}

        private void Follow()
        {
            // 월드 기준 오프셋 계산
            Vector3 forward = _player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward; // 안전장치
            forward.Normalize();

            // 카메라가 위치할 목표점 = 플레이어의 뒤(-forward)로 _backOffset, 위로 _height
            Vector3 wantedPos = _player.position
                              - forward * _backOffset
                              + Vector3.up * _height;

            // 위치 스무딩
            _minimapCamera.transform.position = Vector3.Lerp(
                _minimapCamera.transform.position,
                wantedPos,
                1f - Mathf.Exp(-_posSmooth * Time.deltaTime)
            );

            // 맵이 회전할 때
            if (_rotateWithPlayer)
            {
                // 시선은 플레이어 앞쪽(lookAhead 적용)으로
                Vector3 lookTarget = _player.position + forward * _lookAhead;
                Vector3 camToTarget = lookTarget - _minimapCamera.transform.position;
                camToTarget.y = Mathf.Max(0.01f, camToTarget.y); // 아래쪽 바라보는 값 방지

                // 목표 yaw는 플레이어 진행방향, pitch는 고정
                float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                Quaternion wantedRot = Quaternion.Euler(_pitchDeg, yaw, 0f);

                _minimapCamera.transform.rotation = Quaternion.Slerp(
                    _minimapCamera.transform.rotation,
                    wantedRot,
                    1f - Mathf.Exp(-_rotSmooth * Time.deltaTime)
                );
            }
            else // 맵 회전이 고정일 때
            {
                Quaternion wantedRot = Quaternion.Euler(_pitchDeg, 0f, 0f);
                _minimapCamera.transform.rotation = Quaternion.Slerp(
                    _minimapCamera.transform.rotation,
                    wantedRot,
                    1f - Mathf.Exp(-_rotSmooth * Time.deltaTime)
                );
            }
        }

        // 줌 기능, 속도에 따라 사용할 수 있을 듯
        public void SetDynamicZoom(float speed, float minSize = 70f, float maxSize = 120f, float maxSpeed = 60f)
        {
            if (_minimapCamera == null) return;
            float t = Mathf.Clamp01(speed / maxSpeed);
            float size = Mathf.Lerp(minSize, maxSize, t);
            _minimapCamera.orthographicSize = Mathf.Lerp(_minimapCamera.orthographicSize, size, 0.1f);
        }
    }
}
