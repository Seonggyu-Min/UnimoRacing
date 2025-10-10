using System.Collections;
using System.Reflection;
using UnityEngine;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class SwitchPad : MonoBehaviour
    {
        [System.Serializable]
        public struct PrefabRotation
        {
            [Tooltip("체크 시 아래 EulerAngles 값으로 덮어씁니다.")]
            public bool overrideRotation;

            [Tooltip("절대각도")]
            public Vector3 eulerAngles;
        }

        [Header("프리팹")]
        [SerializeField] private GameObject boosterPrefab;
        [SerializeField] private GameObject slowPrefab;

        [Header("각도 설정")]
        [SerializeField] private PrefabRotation boosterRotation;
        [SerializeField] private PrefabRotation slowRotation;

        private bool isBooster = true;      // 시작은 부스터
        private bool armed = false;         // 생성 직후 1프레임 무시
        private bool swapping = false;      // 교체 중복 방지
        private const float SwapDelay = 1f; // 밟은 후 1초 뒤 교체

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c) c.isTrigger = true;
        }

        private void Start()
        {
            StartCoroutine(ArmNextFrame());
        }

        private IEnumerator ArmNextFrame()
        {
            yield return null;
            armed = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!armed || swapping) return;
            if (!other.CompareTag("Player")) return;
            StartCoroutine(SwapAfterDelay());
        }

        private IEnumerator SwapAfterDelay()
        {
            swapping = true;
            yield return new WaitForSeconds(SwapDelay);
            yield return SwapNowSafely();
        }

        private Quaternion GetSpawnRotation(Transform current, bool spawningBooster)
        {
            PrefabRotation pr = spawningBooster ? boosterRotation : slowRotation;
            if (!pr.overrideRotation)
                return current.rotation;

            return Quaternion.Euler(pr.eulerAngles);
        }

        private IEnumerator SwapNowSafely()
        {
            bool spawningBooster = !isBooster;
            GameObject nextPrefab = isBooster ? slowPrefab : boosterPrefab;
            if (nextPrefab == null) { swapping = false; yield break; }

            Transform t = transform;
            Quaternion spawnRot = GetSpawnRotation(t, spawningBooster);

            GameObject clone = Instantiate(nextPrefab, t.position, spawnRot, t.parent);
            clone.transform.localScale = t.localScale;

            var alt = clone.GetComponent<SwitchPad>();
            if (alt == null) alt = clone.AddComponent<SwitchPad>();
            alt.boosterPrefab = boosterPrefab;
            alt.slowPrefab = slowPrefab;
            alt.boosterRotation = boosterRotation;
            alt.slowRotation = slowRotation;
            alt.isBooster = !isBooster;
            alt.armed = false;
            alt.swapping = false;
            alt.StartCoroutine(alt.ArmNextFrame());

            var col = GetComponent<Collider>();
            if (col) col.enabled = false;

            float extraWait = 0f;
            var slowPad = GetComponent("PJW.SlowPad");
            if (slowPad != null)
            {
                var durField = slowPad.GetType().GetField("duration", BindingFlags.NonPublic | BindingFlags.Instance);
                if (durField != null && durField.FieldType == typeof(float))
                {
                    float dur = (float)durField.GetValue(slowPad);
                    if (dur > 0f) extraWait = dur + 0.05f;
                }
                else
                {
                    extraWait = 2.05f;
                }
            }
            if (extraWait > 0f)
                yield return new WaitForSeconds(extraWait);

            Destroy(gameObject);
        }
    }
}
