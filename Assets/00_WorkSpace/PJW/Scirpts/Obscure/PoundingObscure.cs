using UnityEngine;
using Cinemachine;

namespace PJW
{
    [DisallowMultipleComponent]
    public class PoundingObscure : MonoBehaviour
    {
        [Header("왕복 위치 (Y)")]
        [SerializeField] private float topLocalY = 3f;
        [SerializeField] private float bottomLocalY = 0f;

        [Header("왕복 위치 (Z)")]
        [SerializeField] private float frontLocalZ = 1f;  
        [SerializeField] private float backLocalZ = -1f;  

        [Header("타이밍")]
        [SerializeField] private float dropTime = 0.25f;
        [SerializeField] private float riseTime = 0.6f;
        [SerializeField] private float waitAtTop = 0.4f;
        [SerializeField] private float waitAtBottom = 0.1f;

        [Header("카메라 흔들림(반경/세기)")]
        [SerializeField] private float slamStrength = 1.2f;

        [Header("옵션")]
        [SerializeField] private AnimationCurve dropCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private Transform visual;

        private CinemachineImpulseSource impulse;

        private float baseLocalY;
        private float baseLocalZ;

        private void Reset()
        {
            if (visual == null) visual = transform;
            topLocalY = 3f;
            bottomLocalY = 0f;
            frontLocalZ = 1f;
            backLocalZ = -1f;
            dropTime = 0.25f;
            riseTime = 0.6f;
            waitAtTop = 0.4f;
            waitAtBottom = 0.1f;
            slamStrength = 1.2f;
        }

        private void Awake()
        {
            if (visual == null) visual = transform;

            impulse = GetComponent<CinemachineImpulseSource>();
            if (impulse == null) impulse = gameObject.AddComponent<CinemachineImpulseSource>();
        }

        private void OnEnable()
        {
            Vector3 lp = visual.localPosition;
            baseLocalY = lp.y;
            baseLocalZ = lp.z;

            // 시작 시 상단 위치로 설정
            lp.y = baseLocalY + topLocalY;
            lp.z = baseLocalZ + frontLocalZ;
            visual.localPosition = lp;

            StopAllCoroutines();
            StartCoroutine(RunLoop());
        }

        private System.Collections.IEnumerator RunLoop()
        {
            var waitTop = new WaitForSeconds(waitAtTop);
            var waitBottom = new WaitForSeconds(waitAtBottom);

            while (true)
            {
                yield return waitTop;
                // 내려가면서 Z 뒤로 이동
                yield return MoveLocalYZ(topLocalY, bottomLocalY, frontLocalZ, backLocalZ, dropTime, dropCurve);

                GenerateSlamImpulse();

                yield return waitBottom;
                // 다시 올라가면서 Z 앞으로 이동
                yield return MoveLocalYZ(bottomLocalY, topLocalY, backLocalZ, frontLocalZ, riseTime, riseCurve);
            }
        }

        private System.Collections.IEnumerator MoveLocalYZ(float fromY, float toY, float fromZ, float toZ, float time, AnimationCurve curve)
        {
            float t = 0f;
            Vector3 lp = visual.localPosition;

            float startY = baseLocalY + fromY;
            float endY = baseLocalY + toY;

            float startZ = baseLocalZ + fromZ;
            float endZ = baseLocalZ + toZ;

            while (t < time)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / time);
                float e = curve.Evaluate(k);

                lp.y = Mathf.LerpUnclamped(startY, endY, e);
                lp.z = Mathf.LerpUnclamped(startZ, endZ, e);
                visual.localPosition = lp;

                yield return null;
            }

            lp.y = endY;
            lp.z = endZ;
            visual.localPosition = lp;
        }

        private void GenerateSlamImpulse()
        {
            if (impulse != null)
            {
                Vector3 impactVelocity = Vector3.down * slamStrength;
                impulse.GenerateImpulseAt(visual.position, impactVelocity);
            }
        }
    }
}
