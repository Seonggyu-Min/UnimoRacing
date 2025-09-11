using System.Collections;
using UnityEngine;

namespace PJW
{
    /// <summary>
    /// PlayerRaceData의 KartSpeed를 일정 시간 동안 배수로 올렸다가 원복하는
    /// 최소 기능 부스터 아이템.
    /// </summary>
    public class BoostItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private float speedMultiplier = 1.5f; // 가속 배수
        [SerializeField] private float duration = 3f;          // 유지 시간(초)

        public void Use(GameObject owner)
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            var raceData = owner.GetComponentInParent<PlayerRaceData>();
            if (raceData == null)
            {
                Debug.LogWarning("[BoostItem] PlayerRaceData를 찾지 못했습니다.");
                Destroy(gameObject);
                return;
            }

            StartCoroutine(BoostRoutine(raceData));
            Destroy(gameObject); // 아이템은 사용 즉시 제거
        }

        private IEnumerator BoostRoutine(PlayerRaceData data)
        {
            float original = data.KartSpeed;
            float boosted = original * Mathf.Max(0f, speedMultiplier);

            data.SetKartSpeed(boosted);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                // 중간에 다른 시스템이 바꿔도 부스터 시간 동안은 유지
                data.SetKartSpeed(boosted);
                yield return null;
            }

            data.SetKartSpeed(original); // 원복
        }
    }
}
