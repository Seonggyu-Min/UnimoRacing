using UnityEngine;
using Cinemachine;

public class DollyCartFollower : MonoBehaviour
{
    public CinemachineDollyCart dollyCart;
    public CinemachineSmoothPath path;
    public Transform target; // ����

    public int sampleCount = 200; // ���е� ���̱�
    public float followSpeed = 5f;

    void Update()
    {
        if (dollyCart == null || path == null || target == null) return;

        float closestDistance = float.MaxValue;
        float closestPathPosition = 0f;

        // ���� ������ ��������� ����
        CinemachinePathBase.PositionUnits units = CinemachinePathBase.PositionUnits.Distance;
        float pathLength = path.PathLength;

        // 0���� ��ü �Ÿ����� ���� �������� üũ
        for (int i = 0; i <= sampleCount; i++)
        {
            float t = (pathLength / sampleCount) * i;
            Vector3 posOnPath = path.EvaluatePositionAtUnit(t, units);
            float dist = Vector3.Distance(target.position, posOnPath);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestPathPosition = t;
            }
        }

        // ������ ���� - �ʹ� ����� ��쿡�� �ּ� �̵� �߻�
        float newPosition = Mathf.Lerp(dollyCart.m_Position, closestPathPosition, Time.deltaTime * followSpeed);
        dollyCart.m_Position = Mathf.Clamp(newPosition, 0f, pathLength); // Ʈ�� ���� �ʰ� ����
    }
}