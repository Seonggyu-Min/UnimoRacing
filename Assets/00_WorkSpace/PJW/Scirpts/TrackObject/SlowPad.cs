using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    [RequireComponent(typeof(Collider))]
    public class SlowPad : MonoBehaviour
    {
        [Header("감속 설정")]
        [Range(0f, 1f)]
        [SerializeField] private float slowMultiplier = 0.5f; // 0~1, 예: 0.5 = 절반 속도
        [SerializeField] private float duration = 2f;
        [SerializeField] private string requiredTag = "Player";

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
            if (data == null) return;

            StartCoroutine(SlowRoutine(data));
        }

        private IEnumerator SlowRoutine(PlayerRaceData data)
        {
            float baseSpeed = data.KartSpeed;
            float slowed = baseSpeed * slowMultiplier;

            data.SetKartSpeed(slowed);
            yield return new WaitForSeconds(duration);
            data.SetKartSpeed(baseSpeed);
        }
    }
}
