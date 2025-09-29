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
        [SerializeField] private float localCooldown = 0.06f;          // 같은 패드에서 같은 플레이어 재트리거 무시 시간
        [SerializeField] private bool refreshDurationOnRetrigger = true; // 재트리거 시 남은 시간 갱신

        // 플레이어별 단일 슬로우 관리 (배수만 적용/해제)
        private static readonly Dictionary<int, Coroutine> routineByActor = new Dictionary<int, Coroutine>();
        private static readonly Dictionary<int, float> lastTriggerTimeByActor = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> appliedMulByActor = new Dictionary<int, float>();

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

            // 로컬 쿨다운
            float now = Time.time;
            if (lastTriggerTimeByActor.TryGetValue(actor, out float lastT) && (now - lastT) < localCooldown)
                return;
            lastTriggerTimeByActor[actor] = now;

            float mul = Mathf.Clamp(slowMultiplier, 0.0001f, 1f);

            // 처음 들어왔을 때만 실제 감속 곱하기
            bool firstApply = !appliedMulByActor.ContainsKey(actor);
            if (firstApply)
            {
                data.SetKartSpeed(data.KartSpeed * mul);
                appliedMulByActor[actor] = mul;
            }

            // 코루틴 갱신/재시작
            if (routineByActor.TryGetValue(actor, out var co) && co != null)
            {
                if (refreshDurationOnRetrigger)
                {
                    StopCoroutine(co);
                    routineByActor[actor] = StartCoroutine(SlowRoutine(actor, data, duration));
                }
                // else: 남은 시간을 그대로 둔다
            }
            else
            {
                routineByActor[actor] = StartCoroutine(SlowRoutine(actor, data, duration));
            }
        }

        private IEnumerator SlowRoutine(int actor, PlayerRaceData data, float runTime)
        {
            float end = Time.time + Mathf.Max(0f, runTime);
            while (data && Time.time < end)
                yield return null;

            // 종료: 이번 슬로우가 곱한 배수만 정확히 나누기
            if (data && appliedMulByActor.TryGetValue(actor, out float mul) && mul > 0f)
                data.SetKartSpeed(data.KartSpeed / mul);

            // 정리
            appliedMulByActor.Remove(actor);
            routineByActor.Remove(actor);
        }
    }
}
