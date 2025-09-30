using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class BoosterPad : MonoBehaviour
    {
        [Header("부스터 설정(배수)")]
        [SerializeField] private float boostMultiplier = 1.15f; // 1 = 변화 없음, 1.15 = +15%
        [SerializeField] private float duration = 2f;
        [SerializeField] private string targetTagName = "Player";

        [Header("중복 트리거 쿨다운(같은 패드 내)")]
        [SerializeField] private float localCooldown = 0.06f; // 같은 패드에서 같은 플레이어 재트리거 무시 시간

        // pad-로컬: 플레이어별 마지막 트리거 시각
        private readonly Dictionary<int, float> lastTriggerByActor = new Dictionary<int, float>();

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 태그 필터
            if (!string.IsNullOrEmpty(targetTagName) && !other.CompareTag(targetTagName))
                return;

            // 로컬 소유자만 처리
            var pv = other.GetComponentInParent<PhotonView>();
            if (pv == null || !pv.IsMine)
                return;

            var raceData = other.GetComponentInParent<PlayerRaceData>();
            if (raceData == null)
                return;

            // 같은 패드에서 같은 플레이어가 너무 빨리 또 트리거되면 무시
            int actor = pv.OwnerActorNr;
            float now = Time.time;
            if (lastTriggerByActor.TryGetValue(actor, out float lastT))
            {
                if (now - lastT < localCooldown)
                    return;
            }
            lastTriggerByActor[actor] = now;

            // 비정상 값 방어
            float factor = Mathf.Max(0.01f, boostMultiplier); // 0 또는 음수 방지
            if (Mathf.Approximately(factor, 1f))
            {
                // 변화 없음: 그래도 타이머 리프레시는 가능하게 하려면 주석 해제
                // var s0 = raceData.GetComponent<PadBoostState>() ?? raceData.gameObject.AddComponent<PadBoostState>();
                // s0.ApplyOrRefreshPadBoost(raceData, 1f, duration);
                return;
            }

            var state = raceData.GetComponent<PadBoostState>();
            if (state == null) state = raceData.gameObject.AddComponent<PadBoostState>();

            state.ApplyOrRefreshPadBoost(raceData, factor, duration);
        }
    }

    [DisallowMultipleComponent]
    public class PadBoostState : MonoBehaviour
    {
        private bool active;
        private float currentFactor = 1f;
        private Coroutine timerRoutine;

        // 동일 프레임 다중 호출 방어
        private int lastProcessedFrame = -1;

        /// <summary>
        /// newFactor: 1.0=변화없음, 1.1=+10%, 0.8=-20% 등
        /// 적용 시 ×newFactor, 해제 시 ÷currentFactor.
        /// 갱신 시에는 ratio = newFactor/currentFactor 만큼만 곱해 교체.
        /// </summary>
        public void ApplyOrRefreshPadBoost(PlayerRaceData raceData, float newFactor, float duration)
        {
            if (raceData == null) return;

            // 동일 프레임 재호출 무시
            if (lastProcessedFrame == Time.frameCount) return;
            lastProcessedFrame = Time.frameCount;

            // 비정상 값 방어
            if (newFactor <= 0f) newFactor = 0.01f;

            if (active)
            {
                // 이미 활성화된 상태에서 값이 바뀌면 비율로 교체
                if (!Mathf.Approximately(newFactor, currentFactor))
                {
                    float ratio = newFactor / currentFactor;
                    raceData.SetKartSpeed(raceData.KartSpeed * ratio);
                    currentFactor = newFactor;
                }

                // 타이머 리셋
                if (timerRoutine != null) StopCoroutine(timerRoutine);
                timerRoutine = StartCoroutine(Timer(raceData, duration));
                return;
            }

            // 최초 적용: ×newFactor
            raceData.SetKartSpeed(raceData.KartSpeed * newFactor);
            currentFactor = newFactor;
            active = true;

            // 타이머 시작/리셋
            if (timerRoutine != null) StopCoroutine(timerRoutine);
            timerRoutine = StartCoroutine(Timer(raceData, duration));
        }

        private IEnumerator Timer(PlayerRaceData raceData, float duration)
        {
            yield return new WaitForSeconds(duration);

            // 해제: ÷currentFactor (활성 상태에서만)
            if (active && currentFactor > 0f)
            {
                raceData.SetKartSpeed(raceData.KartSpeed / currentFactor);
            }

            active = false;
            currentFactor = 1f;
            timerRoutine = null;
        }

        private void OnDisable()
        {
            // 중도 비활성화/파괴 시에도 역수로 되돌려 영구 변화 방지
            if (active && currentFactor > 0f)
            {
                var rd = GetComponent<PlayerRaceData>();
                if (rd != null)
                {
                    rd.SetKartSpeed(rd.KartSpeed / currentFactor);
                }
            }

            active = false;
            currentFactor = 1f;

            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }
        }
    }
}
