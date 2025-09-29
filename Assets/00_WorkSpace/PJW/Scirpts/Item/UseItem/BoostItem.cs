using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YTW;

namespace PJW
{
    public class BoostItem : MonoBehaviour, IUsableItem
    {
        [Header("부스트 파라미터")]
        [SerializeField] private float speedMultiplier = 2f;
        [SerializeField] private float duration = 2f;

        [Header("사운드 키")]
        [SerializeField] private string sfxUse = "Boost_Start_SFX";
        [SerializeField] private string sfxActive = "Boost_SFX";
        [SerializeField] private string sfxEnd = "Boost_End_SFX";

        [Header("이펙트 프리팹")]
        [SerializeField] private GameObject boostVfxPrefab;

        [Tooltip("플레이어 로컬 기준 오프셋")]
        [SerializeField] private Vector3 vfxLocalOffset = new Vector3(0f, 0.25f, -0.8f);

        [Tooltip("파티클을 진행 방향으로 회전시킬지 여부 (true=앞, false=뒤)")]
        [SerializeField] private bool alignToForward = false;

        // 배우(플레이어)별 현재 진행 중인 부스트
        private static readonly Dictionary<int, Running> running = new Dictionary<int, Running>();

        private class Running
        {
            public float appliedMul;   // 이번 부스트가 곱한 배수
            public Coroutine routine;
        }

        public void Use(GameObject owner)
        {
            if (!owner) { Destroy(gameObject); return; }

            var data = owner.GetComponentInParent<PlayerRaceData>();
            if (!data) { Destroy(gameObject); return; }

            int key = GetOwnerKey(owner, data);

            // 이전 부스트가 돌고 있으면 '그 부스트가 곱한 배수'만 깔끔히 제거 후 교체
            if (running.TryGetValue(key, out var cur) && cur?.routine != null)
            {
                data.StopCoroutine(cur.routine);
                if (cur.appliedMul > 0f)
                    data.SetKartSpeed(data.KartSpeed / cur.appliedMul); // 역연산으로만 복귀
                running.Remove(key);
            }

            // 사용 즉시 SFX
            if (!string.IsNullOrEmpty(sfxUse))
                AudioManager.Instance.PlaySFX(sfxUse);

            // VFX
            if (boostVfxPrefab != null)
            {
                var anchor = owner.transform;
                Vector3 worldPos = anchor.TransformPoint(vfxLocalOffset);
                Vector3 dir = alignToForward ? anchor.forward : -anchor.forward;
                Quaternion rot = Quaternion.LookRotation(dir, anchor.up);
                var vfx = Object.Instantiate(boostVfxPrefab, worldPos, rot, anchor);
                Object.Destroy(vfx, duration);
            }

            float mul = Mathf.Max(1f, speedMultiplier);

            // 부스트 적용: 곱하기
            data.SetKartSpeed(data.KartSpeed * mul);

            // Active SFX 지연 재생
            if (!string.IsNullOrEmpty(sfxActive))
                data.StartCoroutine(PlayDelayedSfx(sfxActive, 0.5f));

            var slot = new Running { appliedMul = mul };
            slot.routine = data.StartCoroutine(BoostRoutine(data, key, slot, duration, sfxEnd));
            running[key] = slot;

            Destroy(gameObject);
        }

        private static IEnumerator BoostRoutine(PlayerRaceData data, int key, Running slot, float dur, string endSfx)
        {
            float end = Time.unscaledTime + Mathf.Max(0f, dur);
            while (data && Time.unscaledTime < end)
                yield return null;

            if (data)
            {
                // 부스트 해제: 정확히 이번 배수만 나누기
                if (slot.appliedMul > 0f)
                    data.SetKartSpeed(data.KartSpeed / slot.appliedMul);

                if (!string.IsNullOrEmpty(endSfx))
                    AudioManager.Instance.PlaySFX(endSfx);
            }

            running.Remove(key);
        }

        private static IEnumerator PlayDelayedSfx(string key, float delay)
        {
            yield return new WaitForSeconds(delay);
            AudioManager.Instance.PlaySFX(key);
        }

        private static int GetOwnerKey(GameObject owner, PlayerRaceData data)
        {
            var pv = owner.GetComponentInParent<PhotonView>() ?? data.GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null) return pv.OwnerActorNr;
            return data.GetInstanceID();
        }
    }
}
