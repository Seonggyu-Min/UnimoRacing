using UnityEngine;
using Cinemachine;

namespace PJW
{
    public class ParticleSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject pickupVfxPrefab;

        [Header("스폰 설정 (월드 좌표 기준)")]
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;      // 위치 오프셋
        [SerializeField] private Vector3 spawnEulerAngles = Vector3.zero; // 회전 각도

        private bool hasSpawned;

        private void OnTriggerEnter(Collider other)
        {
            if (hasSpawned) return;
            if (other.GetComponentInParent<CinemachineDollyCart>() == null) return;
            if (pickupVfxPrefab == null) return;

            // 위치 + 회전 보정
            Vector3 spawnPos = transform.position + spawnOffset;
            Quaternion spawnRot = Quaternion.Euler(spawnEulerAngles);

            var vfx = Instantiate(pickupVfxPrefab, spawnPos, spawnRot);
            var ps = vfx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < ps.Length; i++) ps[i].Play();

            hasSpawned = true;
        }
    }
}
