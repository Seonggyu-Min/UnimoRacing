using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class BoosterPad : MonoBehaviour
    {
        [Header("부스터 설정")]
        [SerializeField] private float boostAmount = 5f;     
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
            if (!string.IsNullOrEmpty(targetTagName) && !other.CompareTag(targetTagName))
                return;

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

            var state = raceData.GetComponent<PadBoostState>();
            if (state == null) state = raceData.gameObject.AddComponent<PadBoostState>();

            state.ApplyOrRefreshPadBoost(raceData, boostAmount, duration);
        }
    }

    [DisallowMultipleComponent]
    public class PadBoostState : MonoBehaviour
    {
        private bool active;
        private float currentAmount;
        private Coroutine timerRoutine;

        // 동일 프레임 다중 호출 방어
        private int lastProcessedFrame = -1;

        public void ApplyOrRefreshPadBoost(PlayerRaceData raceData, float newAmount, float duration)
        {
            // 동일 프레임에 여러 번 호출되면 한 번만 처리
            if (lastProcessedFrame == Time.frameCount) return;
            lastProcessedFrame = Time.frameCount;

            if (active)
            {
                float delta = newAmount - currentAmount;
                if (Mathf.Abs(delta) > Mathf.Epsilon)
                {
                    raceData.SetKartSpeed(raceData.KartSpeed + delta);
                    currentAmount = newAmount;
                }

                // 타이머 리셋
                if (timerRoutine != null) StopCoroutine(timerRoutine);
                timerRoutine = StartCoroutine(Timer(raceData, duration));
                return;
            }

            // 최초 적용: 1회분만 가산
            raceData.SetKartSpeed(raceData.KartSpeed + newAmount);
            currentAmount = newAmount;
            active = true;

            if (timerRoutine != null) StopCoroutine(timerRoutine);
            timerRoutine = StartCoroutine(Timer(raceData, duration));
        }

        private IEnumerator Timer(PlayerRaceData raceData, float duration)
        {
            yield return new WaitForSeconds(duration);

            // 종료 시 적용량만큼 정확히 제거
            raceData.SetKartSpeed(raceData.KartSpeed - currentAmount);

            active = false;
            currentAmount = 0f;
            timerRoutine = null;
        }

        private void OnDisable()
        {
            // 비활성화/파괴 시에도 남은 적용량을 제거(영구 증가 보호)
            if (active)
            {
                var rd = GetComponent<PlayerRaceData>();
                if (rd != null)
                {
                    rd.SetKartSpeed(rd.KartSpeed - currentAmount);
                }
            }
            active = false;
            currentAmount = 0f;

            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }
        }
    }
}
