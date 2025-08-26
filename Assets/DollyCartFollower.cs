using UnityEngine;
using Cinemachine;

public class DollyCartFollower : MonoBehaviour
{
    public CinemachineDollyCart dollyCart;
    public CinemachineSmoothPath path;
    public Transform target; // 차량

    public int sampleCount = 200; // 정밀도 높이기
    public float followSpeed = 5f;

    void Update()
    {
        if (dollyCart == null || path == null || target == null) return;

        float closestDistance = float.MaxValue;
        float closestPathPosition = 0f;

        // 유닛 단위를 명시적으로 맞춤
        CinemachinePathBase.PositionUnits units = CinemachinePathBase.PositionUnits.Distance;
        float pathLength = path.PathLength;

        // 0부터 전체 거리까지 일정 간격으로 체크
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

        // 안전한 보간 - 너무 가까운 경우에도 최소 이동 발생
        float newPosition = Mathf.Lerp(dollyCart.m_Position, closestPathPosition, Time.deltaTime * followSpeed);
        dollyCart.m_Position = Mathf.Clamp(newPosition, 0f, pathLength); // 트랙 길이 초과 방지
    }
}