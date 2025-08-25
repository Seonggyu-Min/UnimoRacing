using UnityEngine;
using System.Collections.Generic;
using Cinemachine;

public class DollyCartSpeedZoneByWaypoint : MonoBehaviour
{
    [System.Serializable]
    public class SlowZone
    {
        public int startWaypoint;  // ���� ���� �ε���
        public int endWaypoint;    // ���� ���� �ε���
    }

    public CinemachineDollyCart dollyCart;
    public CinemachineSmoothPath path;

    [Header("Speed Settings")]
    public float normalSpeed = 10f;
    public float slowSpeed = 3f;
    public float transitionSpeed = 2f;

    [Header("Slow Zones (by Waypoint Index)")]
    public List<SlowZone> slowZones = new List<SlowZone>();

    private float[] cumulativeDistances; // �� ��������Ʈ������ ���� �Ÿ�

    void Start()
    {
        PrecomputeDistances();
    }

    void Update()
    {
        if (dollyCart == null || path == null || cumulativeDistances == null) return;

        float pos = dollyCart.m_Position; // ���� �Ÿ� ���� ��ġ
        bool isInSlowZone = false;

        foreach (var zone in slowZones)
        {
            if (zone.startWaypoint < 0 || zone.endWaypoint >= cumulativeDistances.Length) continue;

            float startDist = cumulativeDistances[zone.startWaypoint];
            float endDist = cumulativeDistances[zone.endWaypoint];

            if (pos >= startDist && pos <= endDist)
            {
                isInSlowZone = true;
                break;
            }
        }

        float targetSpeed = isInSlowZone ? slowSpeed : normalSpeed;
        dollyCart.m_Speed = Mathf.Lerp(dollyCart.m_Speed, targetSpeed, Time.deltaTime * transitionSpeed);
    }

    void PrecomputeDistances()
    {
        int count = path.m_Waypoints.Length;
        cumulativeDistances = new float[count];
        cumulativeDistances[0] = 0f;

        for (int i = 1; i < count; i++)
        {
            Vector3 p0 = path.m_Waypoints[i - 1].position;
            Vector3 p1 = path.m_Waypoints[i].position;
            float segmentLength = Vector3.Distance(p0, p1);
            cumulativeDistances[i] = cumulativeDistances[i - 1] + segmentLength;
        }
    }
}