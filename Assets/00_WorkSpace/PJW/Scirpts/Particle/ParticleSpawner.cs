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

        [Header("동작 모드")]
        [SerializeField] private bool spawnOnTrigger = true; // true = 닿았을 때, false = 설치하자마자

        private bool hasSpawned;

        private void Start()
        {
            if (!spawnOnTrigger)
            {
                TrySpawnVfx();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!spawnOnTrigger) return; 

            if (other.GetComponentInParent<CinemachineDollyCart>() == null) return;

            TrySpawnVfx();
        }

        private void TrySpawnVfx()
        {
            if (hasSpawned) return;
            if (pickupVfxPrefab == null) return;

            // 위치 + 회전 보정
            Vector3 spawnPos = transform.position + spawnOffset;
            Quaternion spawnRot = pickupVfxPrefab.transform.rotation * Quaternion.Euler(spawnEulerAngles);

            var vfx = Instantiate(pickupVfxPrefab, spawnPos, spawnRot);

            var ps = vfx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < ps.Length; i++) ps[i].Play();

            hasSpawned = true;
        }
    }
}
