using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace YTW
{
    public class TrackSpline_Test : MonoBehaviour
    {
        [SerializeField] private SplineContainer _splineContainer;
        [SerializeField] private float _laneWidth = 2f;

        private void Awake()
        {
            if (_splineContainer == null)
            {
                _splineContainer = GetComponent<SplineContainer>();
            }
        }

        public (Vector3 position, Vector3 forward, Vector3 up) GetLanePoint(float progress, int laneIndex)
        {
            // 변수를 함수 시작 부분에서 미리 선언하고 기본값으로 초기화합니다.
            Vector3 position = transform.position;
            Vector3 tangent = transform.forward;
            Vector3 upVector = transform.up;

            if (_splineContainer != null)
            {
                // 1. Evaluate 함수를 위한 float3 타입 임시 변수 선언
                float3 tempPosition, tempTangent, tempUpVector;

                // 2. float3 타입으로 Evaluate 함수 호출
                _splineContainer.Evaluate(progress, out tempPosition, out tempTangent, out tempUpVector);

                // 3. float3 결과를 다시 Vector3로 변환하여 저장
                position = tempPosition;
                tangent = tempTangent;
                upVector = tempUpVector;

                // 4. 나머지 계산은 Vector3로 진행
                float offset = (laneIndex - 1.5f) * _laneWidth;
                Vector3 rightVector = Vector3.Cross(tangent, upVector).normalized;

                position += rightVector * offset;
            }

            // 최종 계산된 값을 반환합니다.
            return (position, tangent, upVector);
        }
    }

}