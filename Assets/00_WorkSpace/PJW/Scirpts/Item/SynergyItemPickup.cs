using Photon.Pun;
using UnityEngine;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class SynergyItemPickup : MonoBehaviour
    {
        [SerializeField] private bool destroyOnPickup = true;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var racer = other.GetComponentInParent<PlayerRaceData>();
            if (racer == null) return;

            var pv = racer.GetComponent<PhotonView>();
            if (pv == null || !pv.IsMine) return; // 로컬만 먹게

            racer.TryAddSynergyItemForCurrentSynergy();

            if (destroyOnPickup)
                Destroy(gameObject); // 나한테만 보이던 오브젝트 로컬 제거
        }
    }
}
