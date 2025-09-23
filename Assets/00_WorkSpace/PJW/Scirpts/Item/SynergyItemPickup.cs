using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using YTW; // AudioManager 네임스페이스

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class SynergyItemPickup : MonoBehaviour
    {
        [Header("픽업 동작")]
        [SerializeField] private float respawnAfterSeconds = 5f; 

        [Header("제어 대상")]
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Collider[] targetColliders;
        [SerializeField] private GameObject[] extraObjects;

        [Header("사운드 키")]
        [SerializeField] private string sfxPickupKey = "Special_Item"; // Resources/AudioDB에 등록된 키

        private bool isConsumed;
        private readonly HashSet<int> consumedActors = new HashSet<int>();

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            targetRenderers = GetComponentsInChildren<Renderer>(true);
            targetColliders = GetComponentsInChildren<Collider>(true);
        }

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(true);
            if (targetColliders == null || targetColliders.Length == 0)
                targetColliders = GetComponentsInChildren<Collider>(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isConsumed) return;

            var racer = other.GetComponentInParent<PlayerRaceData>();
            if (racer == null) return;

            var pv = racer.GetComponent<PhotonView>();
            if (pv == null || !pv.IsMine) return;

            if (!consumedActors.Add(pv.OwnerActorNr)) return;

            isConsumed = true;

            // 시너지 카운트 처리
            racer.TryAddSynergyItemForCurrentSynergy();

            // 사운드 재생 (로컬만)
            if (!string.IsNullOrEmpty(sfxPickupKey))
                AudioManager.Instance.PlaySFX(sfxPickupKey);

            // 리스폰 / 파괴 처리
            if (respawnAfterSeconds > 0f)
            {
                ApplyRespawn(false);
                StartCoroutine(CoRespawn());
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void ApplyRespawn(bool active)
        {
            if (targetRenderers != null)
            {
                foreach (var r in targetRenderers) if (r) r.enabled = active;
            }
            if (targetColliders != null)
            {
                foreach (var c in targetColliders) if (c) c.enabled = active;
            }
            if (extraObjects != null)
            {
                foreach (var go in extraObjects) if (go) go.SetActive(active);
            }
        }

        private IEnumerator CoRespawn()
        {
            yield return new WaitForSeconds(respawnAfterSeconds);
            isConsumed = false;
            consumedActors.Clear();
            ApplyRespawn(true);
        }
    }
}
