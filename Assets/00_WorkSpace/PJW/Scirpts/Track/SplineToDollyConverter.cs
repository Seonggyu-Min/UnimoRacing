using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Cinemachine;

namespace PJW
{
    [ExecuteAlways]
    public class SplineToDollyMinimalLimited : MonoBehaviour
    {
        [SerializeField] private SplineContainer sourceSpline;

        private const float SampleSpacingMeters = 1.0f; // 기본 샘플 간격
        private const int MinWaypointCount = 8;       // 너무 적은 경우 방지용
        private const int MaxWaypointCount = 56;    // 최대 웨이포인트 수
        private const int LengthSamples = 400;   // 길이 근사 샘플 수

        [ContextMenu("Convert Spline → Dolly Track (Limited)")]
        public void ConvertNow()
        {
            if (sourceSpline == null || sourceSpline.Splines == null || sourceSpline.Splines.Count == 0)
            {
                Debug.LogError("[SplineToDollyMinimalLimited] No valid SplineContainer.");
                return;
            }

            var spline = sourceSpline.Splines[0];

            // 타겟 경로 오브젝트 준비
            string trackName = $"DollyTrack_from_{sourceSpline.gameObject.name}";
            var pathGo = GameObject.Find(trackName);
            var targetPath = pathGo != null
                ? (pathGo.GetComponent<CinemachineSmoothPath>() ?? pathGo.AddComponent<CinemachineSmoothPath>())
                : new GameObject(trackName, typeof(CinemachineSmoothPath))
                { transform = { position = sourceSpline.transform.position, rotation = sourceSpline.transform.rotation } }
                    .GetComponent<CinemachineSmoothPath>();

            targetPath.m_Looped = spline.Closed;

            // 길이 근사 → 기본 샘플 수
            float estimatedLen = EstimateLengthWorld(sourceSpline, LengthSamples);
            int count = Mathf.Max(
                MinWaypointCount,
                Mathf.CeilToInt(Mathf.Max(estimatedLen, 0.1f) / Mathf.Max(0.01f, SampleSpacingMeters))
            );

            // 최대 개수 제한
            count = Mathf.Min(count, MaxWaypointCount);

            // 샘플링
            var worldPos = SampleWorldPositions(sourceSpline, count, targetPath.m_Looped);
            var waypoints = new CinemachineSmoothPath.Waypoint[worldPos.Count];
            for (int i = 0; i < worldPos.Count; i++)
            {
                Vector3 local = targetPath.transform.InverseTransformPoint(worldPos[i]);
                waypoints[i] = new CinemachineSmoothPath.Waypoint { position = local, roll = 0f };
            }
            targetPath.m_Waypoints = waypoints;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(targetPath);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetPath.gameObject.scene);
#endif
            Debug.Log($"[Limited] WayPoint : {waypoints.Length}(Max : {MaxWaypointCount}), Looped={targetPath.m_Looped}");
        }

        private static List<Vector3> SampleWorldPositions(SplineContainer c, int count, bool looped)
        {
            var list = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                float t = looped ? (float)i / count : (float)i / (count - 1);
                var local = c.EvaluatePosition(t);
                list.Add(c.transform.TransformPoint(local));
            }
            return list;
        }

        private static float EstimateLengthWorld(SplineContainer c, int samples)
        {
            if (samples < 2) samples = 2;
            float len = 0f;
            Vector3 prev = c.transform.TransformPoint(c.EvaluatePosition(0f));
            for (int i = 1; i < samples; i++)
            {
                float t = (float)i / (samples - 1);
                Vector3 p = c.transform.TransformPoint(c.EvaluatePosition(t));
                len += Vector3.Distance(prev, p);
                prev = p;
            }
            return len;
        }
    }
}
