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

        public enum SpawnMode
        {
            OnTrigger,   // 1. 닿았을 때
            OnSpawn,     // 2. 스폰되었을 때 (Start)
            OnUse        // 3. 사용했을 때 (직접 호출 필요)
        }

        [Header("동작 모드")]
        [SerializeField] private SpawnMode spawnMode = SpawnMode.OnTrigger;

        private bool hasSpawned;

        private void Start()
        {
            if (spawnMode == SpawnMode.OnSpawn)
                TrySpawnVfx();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (spawnMode != SpawnMode.OnTrigger) return;

            if (other.GetComponentInParent<CinemachineDollyCart>() == null) return;

            TrySpawnVfx();
        }

        /// <summary>
        /// 외부에서 "사용했을 때" 호출하는 함수
        /// </summary>
        public void SpawnOnUse()
        {
            if (spawnMode == SpawnMode.OnUse)
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
