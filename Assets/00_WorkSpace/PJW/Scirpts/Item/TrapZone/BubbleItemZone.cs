using Google.Impl;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class BubbleItemZone : MonoBehaviourPun
    {
        [Header("¼³Á¤")]
        [SerializeField] private float lockDuration;
        [SerializeField] private float autoDespawnTime;

        private bool isConsumed;

        private void Start()
        {
            if (photonView.IsMine)
            {
                StartCoroutine(AutoDespawnRoutine());
            }
        }

        private IEnumerator AutoDespawnRoutine()
        {
            yield return new WaitForSeconds(autoDespawnTime);
            if (!isConsumed)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isConsumed) return;

            var targetPv = other.GetComponentInParent<PhotonView>();
            if (targetPv == null) return;

            var switcher = targetPv.GetComponent<DollyLaneSwitcher>();
            if (switcher == null) return;

            isConsumed = true;

            photonView.RPC(nameof(RPC_ApplyLaneLockToMe), targetPv.Owner, targetPv.ViewID, lockDuration);

            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }

        [PunRPC]
        private void RPC_ApplyLaneLockToMe(int targetViewId, float duration, PhotonMessageInfo info)
        {
            var targetView = PhotonView.Find(targetViewId);
            if (targetView == null) return;

            var switcher = targetView.GetComponent<DollyLaneSwitcher>();
            if (switcher == null) return;

            switcher.StartCoroutine(LaneLockRoutine(switcher, duration));
        }

        private IEnumerator LaneLockRoutine(MonoBehaviour switcher, float duration)
        {
            if (switcher == null) yield break;

            bool wasEnabled = switcher.enabled; 
            switcher.enabled = false;           

            yield return new WaitForSeconds(duration);

            if (switcher != null)
            {
                switcher.enabled = wasEnabled;
            }
        }
    }
}
