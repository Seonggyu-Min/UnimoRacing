using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    public Transform target;             // ���� ����
    public float distance = 3f;          // ī�޶� �Ÿ�
    public float height = 1f;            // ī�޶� ����
    public float baseRotationSpeed = 0.01f; // �⺻ ȸ�� ���� �ӵ�
    public float speedMultiplier = 0.01f; // �ӵ��� ���� ȸ�� ����
    public float maxDegreesPerSecond = 30f; // �ʴ� �ִ� ȸ�� ���� ����
    public float minSpeedToRotate = 0.1f;  // �ּ� �ӵ� (������ ��� ȸ�� ����)

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

        // ��ġ ���󰡱� (������ �Ÿ��� ���� ����)
        Vector3 targetPosition = target.position - target.forward * distance + Vector3.up * height;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f); // ��ġ �̵��� �ε巴��

        // ���� ���
        if (speed > minSpeedToRotate)
        {
            // ������ ���� ���͸� �ε巴�� ���͸�
            smoothedForward = Vector3.Lerp(smoothedForward, target.forward, 0.05f);
            Quaternion targetRotation = Quaternion.LookRotation(smoothedForward, Vector3.up);

            // ȸ�� �ӵ� ����
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
            float dynamicRotationSpeed = baseRotationSpeed + (speed * speedMultiplier);
            float t = Mathf.Min(1f, (maxDegreesPerSecond * Time.deltaTime * dynamicRotationSpeed) / angleDifference);

            // �ε巴�� ȸ��
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }
    }
}