using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace YTW
{
    [RequireComponent(typeof(Collider))]
    public class WaterSlow : MonoBehaviour
    {
        [Header("감속 설정")]
        [Range(0.01f, 1f)]
        [SerializeField] private float slowMultiplier = 0.5f; // 감속 배율
        [SerializeField] private string requiredTag = "Player";

        // 플레이어별 겹침 카운트(같은 플레이어가 여러 물 콜라이더 안에 들어있을 때)
        private static readonly Dictionary<int, int> overlapCountByActor = new Dictionary<int, int>();
        // 플레이어별 적용된 배수 저장(나갈때 정확히 나누기 위한)
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
            if (pv != null && !pv.IsMine) return;

            var data = other.GetComponentInParent<PlayerRaceData>();
            if (data == null || data.View == null || !data.View.IsMine) return;

            int actor = data.View.OwnerActorNr;
            float mul = Mathf.Clamp(slowMultiplier, 0.0001f, 1f);

            if (!overlapCountByActor.TryGetValue(actor, out int cnt))
                cnt = 0;
            cnt++;
            overlapCountByActor[actor] = cnt;

            // 처음 들어온 것일 때만 감속 적용
            if (cnt == 1)
            {
                data.SetKartSpeed(data.KartSpeed * mul);
                appliedMulByActor[actor] = mul;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
                return;

            var pv = other.GetComponentInParent<PhotonView>();
            if (pv != null && !pv.IsMine) return;

            var data = other.GetComponentInParent<PlayerRaceData>();
            if (data == null || data.View == null || !data.View.IsMine) return;

            int actor = data.View.OwnerActorNr;

            if (!overlapCountByActor.TryGetValue(actor, out int cnt))
                return;

            cnt--;
            if (cnt > 0)
            {
                overlapCountByActor[actor] = cnt;
                return;
            }

            // 카운트가 0이면 완전히 나간 것 -> 원상복구
            overlapCountByActor.Remove(actor);

            if (appliedMulByActor.TryGetValue(actor, out float mul) && mul > 0f)
            {
                data.SetKartSpeed(data.KartSpeed / mul);
                appliedMulByActor.Remove(actor);
            }
        }
    }
}
