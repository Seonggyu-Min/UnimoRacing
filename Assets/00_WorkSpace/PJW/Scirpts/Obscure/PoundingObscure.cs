using UnityEngine;
using Cinemachine;

namespace PJW
{
    [DisallowMultipleComponent]
    public class PoundingObscure : MonoBehaviour
    {
        [Header("왕복 위치)")]
        [SerializeField] private float topLocalY = 3f;
        [SerializeField] private float bottomLocalY = 0f;

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

        private void Reset()
        {
            if (visual == null) visual = transform;
            topLocalY = 3f;
            bottomLocalY = 0f;
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
            lp.y = baseLocalY + topLocalY;
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
                yield return MoveLocalY(topLocalY, bottomLocalY, dropTime, dropCurve);

                GenerateSlamImpulse();

                yield return waitBottom;
                yield return MoveLocalY(bottomLocalY, topLocalY, riseTime, riseCurve);
            }
        }

        private System.Collections.IEnumerator MoveLocalY(float from, float to, float time, AnimationCurve curve)
        {
            float t = 0f;
            Vector3 lp = visual.localPosition;

            float fromY = baseLocalY + from;
            float toY = baseLocalY + to;

            while (t < time)
            {
                t += Time.deltaTime;
                float k = time > 0f ? Mathf.Clamp01(t / time) : 1f;
                float y = Mathf.LerpUnclamped(fromY, toY, curve.Evaluate(k));
                lp.x = visual.localPosition.x;
                lp.z = visual.localPosition.z;
                lp.y = y;
                visual.localPosition = lp;
                yield return null;
            }

            lp = visual.localPosition;
            lp.y = toY;
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
