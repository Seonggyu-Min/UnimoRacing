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
            // ������ �Լ� ���� �κп��� �̸� �����ϰ� �⺻������ �ʱ�ȭ�մϴ�.
            Vector3 position = transform.position;
            Vector3 tangent = transform.forward;
            Vector3 upVector = transform.up;

            if (_splineContainer != null)
            {
                // 1. Evaluate �Լ��� ���� float3 Ÿ�� �ӽ� ���� ����
                float3 tempPosition, tempTangent, tempUpVector;

                // 2. float3 Ÿ������ Evaluate �Լ� ȣ��
                _splineContainer.Evaluate(progress, out tempPosition, out tempTangent, out tempUpVector);

                // 3. float3 ����� �ٽ� Vector3�� ��ȯ�Ͽ� ����
                position = tempPosition;
                tangent = tempTangent;
                upVector = tempUpVector;

                // 4. ������ ����� Vector3�� ����
                float offset = (laneIndex - 1.5f) * _laneWidth;
                Vector3 rightVector = Vector3.Cross(tangent, upVector).normalized;

                position += rightVector * offset;
            }

            // ���� ���� ���� ��ȯ�մϴ�.
            return (position, tangent, upVector);
        }
    }

}