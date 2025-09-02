using UnityEngine;
using Cinemachine;

namespace PJW
{
    public class ParticleSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject pickupVfxPrefab;
        private bool hasSpawned;

        private void OnTriggerEnter(Collider other)
        {
            if (hasSpawned) return;
            if (other.GetComponentInParent<CinemachineDollyCart>() == null) return;
            if (pickupVfxPrefab == null) return;

            var vfx = Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);
            var ps = vfx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < ps.Length; i++) ps[i].Play();

            hasSpawned = true;
        }
    }
}
