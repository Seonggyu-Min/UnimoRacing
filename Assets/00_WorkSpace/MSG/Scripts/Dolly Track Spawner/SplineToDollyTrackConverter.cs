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
    [ExecuteAlways]
    public class SplineToDollyTrackConverter : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private SplineContainer sourceSpline;

        [Header("Bake Precision (Adaptive Subdivision)")]
        [Tooltip("원본 스플라인을 선분으로 근사할 때 허용 오차(미터)")]
        [Min(0.0001f)] public float epsilon = 0.01f;
        [Tooltip("최대 재귀 깊이(적응 분할 한도)")]
        [Range(2, 16)] public int maxDepth = 12;

        [Header("Parallel Tracks")]
        [Min(1)] public int trackCount = 4;
        [Min(0.01f)] public float lateralSpacing = 2.0f;
        public bool centerAligned = true;

        public enum LateralMode { FrameRight, WorldProjected }

        [Header("Lateral Offset Mode")]
        [Tooltip("FrameRight: 뱅크/경사에 따라 레일도 기울어짐\nWorldProjected: 월드 업 기준으로 수평(XZ) 전개")]
        public LateralMode lateralMode = LateralMode.FrameRight;

        [Header("Frame Basis")]
        [Tooltip("초기 업벡터(0이면 sourceSpline.transform.up 사용)")]
        public Vector3 worldUpOverride = Vector3.zero;

        [Header("Loop Options")]
        [Tooltip("루프 경로에서 시작/끝 프레임의 롤 차이를 전체에 분산 보정")]
        public bool fixLoopTwist = true;

        [Header("Naming")]
        public string trackPrefix = "DollyLinear";

        [Button("Convert Spline To Dolly Tracks")]
        public void ConvertNow()
        {
            if (!ValidateSource()) return;

            var spline = sourceSpline.Splines[0];
            bool looped = spline.Closed;

            // 1) ε-정확도 선분 폴리라인 베이크 (월드 좌표)
            var basePts = BakeLinearPolyline(sourceSpline, epsilon, maxDepth, looped);
            if (basePts.Count < 2)
            {
                Debug.LogWarning("[SplineBake] Not enough baked points.");
                return;
            }

            // 2) 초기 업(up0) 안정 생성
            Vector3 t0 = (basePts[1] - basePts[0]);
            if (t0.sqrMagnitude < 1e-12f) t0 = Vector3.forward;
            t0.Normalize();

            Vector3? overrideUpOpt = (worldUpOverride == Vector3.zero)
                ? (Vector3?)null
                : worldUpOverride.normalized;

            Vector3 up0 = BuildInitialUp(t0, overrideUpOpt, sourceSpline.transform);

            // 3) PTF 프레임 계산(T/U/R)
            ComputeParallelTransportFrames(basePts, up0,
                out var tangents, out var ups, out var rights, looped);

            // 3-1) 부호 연속성 U만 보정, R은 항상 cross(T, U)로 재산출
            EnforceFrameContinuity(tangents, ups, rights);

            // 3-2) 루프 트위스트 분산(끝 탄젠트 기준 시그니처 롤)
            if (looped && fixLoopTwist)
                DistributeLoopTwist(tangents, ups);

            // 3-3) 오픈 경로 마지막 프레임 약간 스무딩(시각 튐 완화)
            if (!looped && ups.Length >= 2)
            {
                int n = ups.Length;
                ups[n - 1] = Vector3.Slerp(ups[n - 2], ups[n - 1], 0.75f).normalized;
                tangents[n - 1] = Vector3.Slerp(tangents[n - 2], tangents[n - 1], 0.75f).normalized;
                rights[n - 1] = Vector3.Cross(tangents[n - 1], ups[n - 1]).normalized;
            }

            // 4) 평행 오프셋 세트
            float[] offsets = BuildOffsets(trackCount, lateralSpacing, centerAligned);

            // 5) 트랙 생성 (Linear) + roll 주입
            int wpCount = basePts.Count; // Linear: 루프도 중복점 없이 m_Looped로 연결

            // 롤 계산 기준 업: 항상 동일 기준 유지(override 있으면 그걸, 없으면 source.up)
            Vector3 rollUpRef = (worldUpOverride == Vector3.zero)
                ? sourceSpline.transform.up
                : worldUpOverride.normalized;

            // 월드 업(수평 전개용)
            Vector3 worldUpForProjection = (worldUpOverride == Vector3.zero)
                ? Vector3.up
                : worldUpOverride.normalized;

            for (int t = 0; t < trackCount; t++)
            {
                string name = $"{trackPrefix}_{t}_from_{sourceSpline.gameObject.name}";
                var path = FindOrCreateLinearPath(name);
                path.m_Looped = looped;

                float off = offsets[t];

                var wps = new CinemachinePath.Waypoint[wpCount];
                for (int i = 0; i < wpCount; i++)
                {
                    // 오프셋 축 선택
                    Vector3 side =
                        (lateralMode == LateralMode.WorldProjected)
                        ? Vector3.Cross(worldUpForProjection, tangents[i]).normalized
                        : rights[i];

                    Vector3 wpos = basePts[i] + side * off;

                    // 기준 업 고정으로 롤 산출
                    float rollDeg = ComputeRollDeg(rollUpRef, tangents[i], ups[i]);

                    wps[i] = new CinemachinePath.Waypoint
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

            Debug.Log($"[Parallel+Roll Stable] Tracks={trackCount}, Points={basePts.Count}, Loop={looped}, ε={epsilon}, Lateral={lateralMode}");
        }

        // ───────────────────── Bake (Adaptive Subdivision) ─────────────────────
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

        // ───────────────────── Initial Up (robust) ─────────────────────
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

        // ───────────────────── PTF + Continuity + Loop Fix ─────────────────────
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

        // ───────────────────── Helpers ─────────────────────
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
    }
}
