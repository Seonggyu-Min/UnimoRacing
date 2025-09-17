using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    public Transform target;             // 따라갈 차량
    public float distance = 3f;          // 카메라 거리
    public float height = 1f;            // 카메라 높이
    public float baseRotationSpeed = 0.01f; // 기본 회전 반응 속도
    public float speedMultiplier = 0.01f; // 속도에 따른 회전 보정
    public float maxDegreesPerSecond = 30f; // 초당 최대 회전 각도 제한
    public float minSpeedToRotate = 0.1f;  // 최소 속도 (이하일 경우 회전 생략)

    private Rigidbody targetRb;
    private Vector3 smoothedForward;

    void Start()
    {
        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody>();
            smoothedForward = target.forward;
        }
    }

    void LateUpdate()
    {
        if (target == null || targetRb == null) return;

        float speed = targetRb.velocity.magnitude;

        // 위치 따라가기 (고정된 거리와 높이 유지)
        Vector3 targetPosition = target.position - target.forward * distance + Vector3.up * height;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f); // 위치 이동도 부드럽게

        // 방향 계산
        if (speed > minSpeedToRotate)
        {
            // 차량의 전방 벡터를 부드럽게 필터링
            smoothedForward = Vector3.Lerp(smoothedForward, target.forward, 0.05f);
            Quaternion targetRotation = Quaternion.LookRotation(smoothedForward, Vector3.up);

            // 회전 속도 조절
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
            float dynamicRotationSpeed = baseRotationSpeed + (speed * speedMultiplier);
            float t = Mathf.Min(1f, (maxDegreesPerSecond * Time.deltaTime * dynamicRotationSpeed) / angleDifference);

            // 부드럽게 회전
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }
    }
}