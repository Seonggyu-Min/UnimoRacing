using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[AddComponentMenu("UI/Effects/UI Gradient")]
public sealed class UIGradient : BaseMeshEffect
{
    [Header("Colors")]
    public Color Color1 = Color.white;
    public Color Color2 = Color.white;

    [Header("Angle")]
    [Range(-180f, 180f)]
    public float Angle = -90f;

    [Tooltip("각도를 시간에 따라 회전할지 여부")]
    public bool RotateAngle = false;

    [Min(0.01f)]
    public float AngleRotateSpeed = 30f; // degrees per second

    // --- Lifecycle ----------------------------------------------------------

    protected override void OnEnable()
    {
        base.OnEnable();
        SetDirty();
        StartRotateIfNeeded();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopRotate();
        SetDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetDirty();
        StartRotateIfNeeded();
    }
#endif

    void Update()
    {
        // 에디터에서 실행 안 해도 미세한 변경 반영
        if (!RotateAngle) return;

        // 프레임마다 각도 갱신하고 리빌드 요청
        Angle += (Application.isPlaying ? Time.deltaTime : 0.016f) * AngleRotateSpeed;
        if (Angle > 180f) Angle -= 360f;
        if (Angle < -180f) Angle += 360f;
        SetDirty();
    }

    // --- Mesh Effect --------------------------------------------------------

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        // 각도 → 단위 방향벡터
        var dir = RotationDir(Angle);

        // Rect 기준 로컬 포지션을 0~1로 투영하는 2x3 행렬
        var rect = graphic.rectTransform.rect;
        var m = LocalPositionMatrix(rect, dir);

        // 정점 순회하며 y(=투영값 t)에 따라 Color2→Color1 보간
        var vertex = default(UIVertex);
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            var t = Mathf.Clamp01((m * (Vector2)vertex.position).y);
            var c = Color.Lerp(Color2, Color1, t);

            // 보통은 그라디언트를 "덮어쓰기"가 더 기대에 맞음
            vertex.color = c;

            vh.SetUIVertex(vertex, i);
        }
    }

    // --- Helpers ------------------------------------------------------------

    struct Matrix2x3
    {
        public float m00, m01, m02, m10, m11, m12;
        public Matrix2x3(float m00, float m01, float m02, float m10, float m11, float m12)
        { this.m00 = m00; this.m01 = m01; this.m02 = m02; this.m10 = m10; this.m11 = m11; this.m12 = m12; }

        public static Vector2 operator *(Matrix2x3 m, Vector2 v)
        {
            float x = (m.m00 * v.x) - (m.m01 * v.y) + m.m02;
            float y = (m.m10 * v.x) + (m.m11 * v.y) + m.m12;
            return new Vector2(x, y);
        }
    }

    // Rect의 로컬 좌표를 회전/정규화해서 0~1 범위로 투영
    static Matrix2x3 LocalPositionMatrix(Rect rect, Vector2 dir)
    {
        float cos = dir.x;
        float sin = dir.y;

        Vector2 rectMin = rect.min;
        Vector2 rectSize = rect.size;

        // 중심 기준 정규화 상수(0.5가 이론상 중앙값). 약간 줄여 클리핑 보정 가능.
        const float c = 0.5f;

        float ax = rectMin.x / rectSize.x + c;
        float ay = rectMin.y / rectSize.y + c;

        float m00 =  cos / rectSize.x;
        float m01 =  sin / rectSize.y;
        float m02 = -(ax * cos - ay * sin - c);

        float m10 =  sin / rectSize.x;
        float m11 =  cos / rectSize.y;
        float m12 = -(ax * sin + ay * cos - c);

        return new Matrix2x3(m00, m01, m02, m10, m11, m12);
    }

    static Vector2 RotationDir(float angleDeg)
    {
        float r = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    void SetDirty()
    {
        if (graphic != null)
        {
            graphic.SetVerticesDirty();
            // 색만 바뀌므로 머티리얼은 건드릴 필요 없음
        }
    }

    void StartRotateIfNeeded()
    {
        // Update에서 처리하므로 별도 코루틴은 필요 없음
        if (!RotateAngle) return;
        // 즉시 1프레임 반영
        SetDirty();
    }

    void StopRotate()
    {
        // 코루틴 사용 안 함
    }
}
