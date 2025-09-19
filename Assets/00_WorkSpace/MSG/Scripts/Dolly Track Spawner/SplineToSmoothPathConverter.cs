using Cinemachine;
using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MSG
{
    public class SplineToSmoothPathConverter : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("Source")]
        [SerializeField] private SplineContainer sourceSpline;

        [Header("Bake Precision (Adaptive Subdivision)")]
        [Min(0.0001f)] public float epsilon = 0.01f;
        [Range(2, 16)] public int maxDepth = 12;

        [Header("Parallel Tracks")]
        [Min(1)] public int trackCount = 4;
        [Min(0.01f)] public float lateralSpacing = 2.0f;
        public bool centerAligned = true;

        public enum LateralMode { FrameRight, WorldProjected }

        [Header("Lateral Offset Mode")]
        public LateralMode lateralMode = LateralMode.FrameRight;

        [Header("Frame Basis")]
        public Vector3 worldUpOverride = Vector3.zero;

        [Header("Loop Options")]
        public bool fixLoopTwist = true;

        [Header("Naming")]
        public string trackPrefix = "DollySmooth";

        [Button("Convert Spline To Smooth Dolly Tracks")]
        public void ConvertNow()
        {
            if (!ValidateSource()) return;

            var spline = sourceSpline.Splines[0];
            bool looped = spline.Closed;

            // 1) 원본 스플라인을 폴리라인으로 근사
            var basePts = BakeLinearPolyline(sourceSpline, epsilon, maxDepth, looped);
            if (basePts.Count < 2)
            {
                Debug.LogWarning("[SplineBake] Not enough baked points.");
                return;
            }

            // 2) 초기 업 벡터
            Vector3 t0 = (basePts[1] - basePts[0]);
            if (t0.sqrMagnitude < 1e-12f) t0 = Vector3.forward;
            t0.Normalize();

            Vector3? overrideUpOpt = (worldUpOverride == Vector3.zero) ? (Vector3?)null : worldUpOverride.normalized;
            Vector3 up0 = BuildInitialUp(t0, overrideUpOpt, sourceSpline.transform);

            // 3) PTF 프레임 계산
            ComputeParallelTransportFrames(basePts, up0, out var tangents, out var ups, out var rights, looped);
            EnforceFrameContinuity(tangents, ups, rights);
            if (looped && fixLoopTwist) DistributeLoopTwist(tangents, ups);

            // 4) 오프셋
            float[] offsets = BuildOffsets(trackCount, lateralSpacing, centerAligned);

            // 5) Smooth Dolly Track 생성
            int wpCount = basePts.Count;

            Vector3 rollUpRef = (worldUpOverride == Vector3.zero) ? sourceSpline.transform.up : worldUpOverride.normalized;
            Vector3 worldUpForProjection = (worldUpOverride == Vector3.zero) ? Vector3.up : worldUpOverride.normalized;

            for (int t = 0; t < trackCount; t++)
            {
                string name = $"{trackPrefix}_{t}_from_{sourceSpline.gameObject.name}";
                var path = FindOrCreateSmoothPath(name);
                path.m_Looped = looped;

                float off = offsets[t];
                var wps = new CinemachineSmoothPath.Waypoint[wpCount];

                for (int i = 0; i < wpCount; i++)
                {
                    Vector3 side =
                        (lateralMode == LateralMode.WorldProjected)
                        ? Vector3.Cross(worldUpForProjection, tangents[i]).normalized
                        : rights[i];

                    Vector3 wpos = basePts[i] + side * off;
                    float rollDeg = ComputeRollDeg(rollUpRef, tangents[i], ups[i]);

                    wps[i] = new CinemachineSmoothPath.Waypoint
                    {
                        position = path.transform.InverseTransformPoint(wpos),
                        roll = rollDeg
                    };
                }

                path.m_Waypoints = wps;

#if UNITY_EDITOR
                EditorUtility.SetDirty(path);
                EditorSceneManager.MarkSceneDirty(path.gameObject.scene);
#endif
            }

            Debug.Log($"[Smooth Dolly Tracks] Tracks={trackCount}, Points={basePts.Count}, Loop={looped}, ε={epsilon}, Lateral={lateralMode}");
        }

        // ───────────────────── Helpers ─────────────────────

        private static List<Vector3> BakeLinearPolyline(SplineContainer c, float eps, int depth, bool looped)
        {
            var pts = new List<Vector3>(256);
            var tr = c.transform;

            float t0 = 0f, t1 = 1f;
            Subdivide(tr, c, t0, t1, eps, depth, pts);

            // 루프: 마지막 중복점 제거
            if (looped && pts.Count >= 2 && (pts[0] - pts[^1]).sqrMagnitude < 1e-12f)
                pts.RemoveAt(pts.Count - 1);

            return pts;
        }

        private static void Subdivide(Transform tr, SplineContainer c, float t0, float t1, float eps, int depth, List<Vector3> outPts)
        {
            Vector3 p0 = tr.TransformPoint(c.EvaluatePosition(t0));
            Vector3 p1 = tr.TransformPoint(c.EvaluatePosition(t1));
            float tm = 0.5f * (t0 + t1);
            Vector3 pm = tr.TransformPoint(c.EvaluatePosition(tm));

            float err = DistancePointToSegment(pm, p0, p1);
            if (err > eps && depth > 0)
            {
                Subdivide(tr, c, t0, tm, eps, depth - 1, outPts);
                Subdivide(tr, c, tm, t1, eps, depth - 1, outPts);
            }
            else
            {
                if (outPts.Count == 0) outPts.Add(p0);
                outPts.Add(p1);
            }
        }
        private static float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 <= Mathf.Epsilon) return Vector3.Distance(p, a);
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
            Vector3 q = a + t * ab;
            return Vector3.Distance(p, q);
        }


        private static Vector3 BuildInitialUp(Vector3 t0, Vector3? overrideUpOpt, Transform fallback)
        {
            Vector3 refUp = overrideUpOpt.HasValue ? overrideUpOpt.Value : fallback.up;

            // 접선에 직교 성분만 추출
            Vector3 u0 = Vector3.ProjectOnPlane(refUp, t0).normalized;

            // 투영이 거의 0이면 대체 축 구성
            if (u0.sqrMagnitude < 1e-6f)
            {
                Vector3 right = Vector3.Cross(Vector3.up, t0);
                if (right.sqrMagnitude < 1e-6f) right = Vector3.Cross(Vector3.forward, t0);
                right.Normalize();

                u0 = Vector3.Cross(t0, right).normalized;
            }

            return u0;
        }

        private static void ComputeParallelTransportFrames(
    List<Vector3> pts,
    Vector3 up0,
    out Vector3[] tangents,
    out Vector3[] ups,
    out Vector3[] rights,
    bool looped)
        {
            int n = pts.Count;
            tangents = new Vector3[n];
            ups = new Vector3[n];
            rights = new Vector3[n];

            // 접선
            for (int i = 0; i < n; i++)
            {
                int inext = (i + 1) % n;
                if (!looped && i == n - 1) inext = i;
                Vector3 v = pts[inext] - pts[i];
                tangents[i] = (v.sqrMagnitude > 1e-10f) ? v.normalized : (i > 0 ? tangents[i - 1] : Vector3.forward);
            }

            // 시작 프레임(직교 정규화)
            Vector3 U0 = Vector3.ProjectOnPlane(up0, tangents[0]).normalized;
            if (U0.sqrMagnitude < 1e-8f) U0 = Vector3.up;
            Vector3 R0 = Vector3.Cross(tangents[0], U0).normalized;   // 오른손계
            U0 = Vector3.Cross(R0, tangents[0]).normalized;

            ups[0] = U0;
            rights[0] = R0;

            // 최소 회전 수송
            for (int i = 1; i < n; i++)
            {
                Vector3 Ti_1 = tangents[i - 1];
                Vector3 Ti = tangents[i];

                Vector3 axis = Vector3.Cross(Ti_1, Ti);
                float axisLen = axis.magnitude;

                if (axisLen < 1e-8f)
                {
                    ups[i] = ups[i - 1];
                }
                else
                {
                    axis /= axisLen;
                    float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(Ti_1, Ti), -1f, 1f)) * Mathf.Rad2Deg;
                    ups[i] = Quaternion.AngleAxis(angle, axis) * ups[i - 1];
                }

                rights[i] = Vector3.Cross(tangents[i], ups[i]).normalized;
                ups[i] = Vector3.Cross(rights[i], tangents[i]).normalized;
            }
        }

        // U만 연속성 강제, R은 항상 cross(T, U)로 재계산(오른손계 보장)
        private static void EnforceFrameContinuity(Vector3[] tangents, Vector3[] ups, Vector3[] rights)
        {
            for (int i = 1; i < ups.Length; i++)
            {
                if (Vector3.Dot(ups[i], ups[i - 1]) < 0f)
                    ups[i] = -ups[i];

                rights[i] = Vector3.Cross(tangents[i], ups[i]).normalized;
            }
            rights[0] = Vector3.Cross(tangents[0], ups[0]).normalized;
        }

        // 끝 탄젠트 축을 기준으로 시작/끝 업 사이의 시그니처 롤을 산출해 분산 보정
        private static void DistributeLoopTwist(Vector3[] tangents, Vector3[] ups)
        {
            int n = ups.Length;
            if (n < 2) return;

            float delta = SignedRollDeg(tangents[^1], ups[0], ups[^1]);
            if (Mathf.Abs(delta) < 0.01f) return;

            for (int i = 1; i < n; i++)
            {
                float w = (float)i / (n - 1);
                float ang = -delta * w;
                ups[i] = Quaternion.AngleAxis(ang, tangents[i]) * ups[i];
            }
        }

        // Waypoint.roll 계산용: 기준 업(upRef) 대비 현재 up의 롤(도)
        private static float ComputeRollDeg(Vector3 upRef, Vector3 T, Vector3 up)
        {
            Vector3 u0 = Vector3.ProjectOnPlane(upRef, T).normalized;
            Vector3 u1 = Vector3.ProjectOnPlane(up, T).normalized;
            if (u0.sqrMagnitude < 1e-8f || u1.sqrMagnitude < 1e-8f) return 0f;
            return Vector3.SignedAngle(u0, u1, T);
        }

        // 루프 보정용: 두 업 벡터 사이의 시그니처 롤(도), 축은 T
        private static float SignedRollDeg(Vector3 T, Vector3 upA, Vector3 upB)
        {
            Vector3 u0 = Vector3.ProjectOnPlane(upA, T).normalized;
            Vector3 u1 = Vector3.ProjectOnPlane(upB, T).normalized;
            if (u0.sqrMagnitude < 1e-8f || u1.sqrMagnitude < 1e-8f) return 0f;
            return Vector3.SignedAngle(u0, u1, T);
        }

        private CinemachineSmoothPath FindOrCreateSmoothPath(string name)
        {
            var go = GameObject.Find(name);
            var path = go != null
                ? (go.GetComponent<CinemachineSmoothPath>() ?? go.AddComponent<CinemachineSmoothPath>())
                : new GameObject(name, typeof(CinemachineSmoothPath)).GetComponent<CinemachineSmoothPath>();

            path.transform.SetPositionAndRotation(sourceSpline.transform.position, sourceSpline.transform.rotation);
            return path;
        }

        private bool ValidateSource()
        {
            if (sourceSpline == null || sourceSpline.Splines == null || sourceSpline.Splines.Count == 0)
            {
                Debug.LogError("[SplineBake] No valid SplineContainer.");
                return false;
            }
            return true;
        }

        private static float[] BuildOffsets(int count, float spacing, bool center)
        {
            var offs = new float[count];
            if (center)
            {
                float mid = (count - 1) * 0.5f;
                for (int i = 0; i < count; i++) offs[i] = (i - mid) * spacing;
            }
            else
            {
                for (int i = 0; i < count; i++) offs[i] = i * spacing;
            }
            return offs;
        }

        private CinemachinePath FindOrCreateLinearPath(string name)
        {
            var go = GameObject.Find(name);
            var path = go != null
                ? (go.GetComponent<CinemachinePath>() ?? go.AddComponent<CinemachinePath>())
                : new GameObject(name, typeof(CinemachinePath)).GetComponent<CinemachinePath>();

            // 소스 기준 좌표계로 정렬
            path.transform.SetPositionAndRotation(sourceSpline.transform.position, sourceSpline.transform.rotation);
            return path;
        }

#endif
    }
}
