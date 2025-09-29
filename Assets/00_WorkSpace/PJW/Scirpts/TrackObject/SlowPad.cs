using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    [RequireComponent(typeof(Collider))]
    public class SlowPad : MonoBehaviour
    {
        [Header("감속 설정")]
        [Range(0f, 1f)]
        [SerializeField] private float slowMultiplier = 0.5f; 
        [SerializeField] private float duration = 2f;
        [SerializeField] private string requiredTag = "Player";

        [Header("중복 처리")]
        [SerializeField] private float localCooldown = 0.06f;   // 같은 패드에서 같은 플레이어 재트리거 무시 시간
        [SerializeField] private bool refreshDurationOnRetrigger = true; // 재트리거 시 남은 시간 갱신 여부

        // 모든 슬로우 패드 인스턴스가 공유 (플레이어별 단일 슬로우 관리)
        private static readonly Dictionary<int, float> originalSpeedByActor = new Dictionary<int, float>();
        private static readonly Dictionary<int, Coroutine> routineByActor = new Dictionary<int, Coroutine>();
        private static readonly Dictionary<int, float> lastTriggerTimeByActor = new Dictionary<int, float>();

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
                return;

            var pv = other.GetComponentInParent<PhotonView>();
            if (pv != null && !pv.IsMine) return; // 로컬 소유자만 처리

            var data = other.GetComponentInParent<PlayerRaceData>();
            if (data == null || data.View == null || !data.View.IsMine) return;

            int actor = data.View.OwnerActorNr;

            // 로컬 쿨다운 (같은 패드에서 연속 진입 방지)
            float now = Time.time;
            if (lastTriggerTimeByActor.TryGetValue(actor, out float lastT) && (now - lastT) < localCooldown)
                return;
            lastTriggerTimeByActor[actor] = now;

            // 첫 적용 시에만 "원래 속도" 저장
            if (!originalSpeedByActor.ContainsKey(actor))
                originalSpeedByActor[actor] = data.KartSpeed;

            float targetSpeed = originalSpeedByActor[actor] * slowMultiplier;
            data.SetKartSpeed(targetSpeed);

            // 기존 코루틴이 돌고 있으면 중단
            if (routineByActor.TryGetValue(actor, out var running))
                StopCoroutine(running);

            // 재트리거시 남은 시간을 갱신할지 여부
            float runTime = refreshDurationOnRetrigger ? duration : Mathf.Max(0f, duration);

            routineByActor[actor] = StartCoroutine(SlowRoutine(actor, data, runTime));
        }

        private IEnumerator SlowRoutine(int actor, PlayerRaceData data, float runTime)
        {
            float elapsed = 0f;
            while (elapsed < runTime)
            {
                // 플레이어/오브젝트가 사라지면 안전 종료
                if (data == null) yield break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 종료 시 원래 속도로만 복원 (드리프트 방지)
            if (data != null && originalSpeedByActor.TryGetValue(actor, out float baseSpeed))
            {
                data.SetKartSpeed(baseSpeed);
            }

            // 정리
            originalSpeedByActor.Remove(actor);
            routineByActor.Remove(actor);
        }
    }
}
