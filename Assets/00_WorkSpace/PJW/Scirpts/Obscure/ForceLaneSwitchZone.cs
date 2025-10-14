using UnityEngine;
using Photon.Pun;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class ForceLaneSwitchZone : MonoBehaviour
    {
        [Header("고정 이동 타겟")]
        [SerializeField] private int targetLaneIndex = 1; // 2번 레인을 의미

        [Header("트리거 동작 옵션")]
        [SerializeField] private bool onlyAffectLocalOwner = true; // 로컬 소유자만 적용
        [SerializeField] private bool consumeOnTrigger = false;    // 한 번 밟으면 비활성화
        [SerializeField] private float reuseDelay = 0f;            // 재사용 대기(초). 0이면 즉시 재사용

        private bool isCoolingDown;

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c) c.isTrigger = true;
        }

        private void OnValidate()
        {
            var c = GetComponent<Collider>();
            if (c) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCoolingDown) return;

            var ctrl = other.GetComponentInParent<DollyCartController>();
            if (ctrl == null) return;

            var pv = other.GetComponentInParent<PhotonView>();
            if (onlyAffectLocalOwner && pv != null && !pv.IsMine) return;

            int target = targetLaneIndex;

            // 레인 수를 알면 안전하게 클램프
            var reg = TrackPathRegistry.Instance;
            if (reg != null && reg.IsInit)
            {
                int len = reg.GetPathLength();
                if (len > 0) target = Mathf.Clamp(target, 0, len - 1);
            }

            // 무조건 지정 레인으로 전환 (컨트롤러 내부에서 잠금/파티션/준비 상태 처리)
            ctrl.ChangeTrack(target);

            // 소모/쿨다운 처리
            if (consumeOnTrigger)
            {
                gameObject.SetActive(false);
            }
            else if (reuseDelay > 0f)
            {
                StartCoroutine(CooldownRoutine());
            }
        }

        private System.Collections.IEnumerator CooldownRoutine()
        {
            isCoolingDown = true;
            yield return new WaitForSeconds(reuseDelay);
            isCoolingDown = false;
        }
    }
}
