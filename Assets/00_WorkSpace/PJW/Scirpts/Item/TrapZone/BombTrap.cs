using System.Collections;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    [RequireComponent(typeof(PhotonView))]
    public class BombTrap : MonoBehaviourPun
    {
        [SerializeField] private float stopDuration = 1.5f;
        private bool hasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (!PhotonNetwork.IsMasterClient || hasTriggered)
                return;

            var targetPv = other.GetComponentInParent<PhotonView>();
            if (targetPv == null)
                return;

            hasTriggered = true;

            photonView.RPC(nameof(RpcApplyBombStopOnOwner), targetPv.Owner, stopDuration);
            PhotonNetwork.Destroy(gameObject);
        }

        [PunRPC]
        private void RpcApplyBombStopOnOwner(float duration)
        {
            var myRacer = FindObjectsOfType<PlayerRaceData>(true)
                .FirstOrDefault(r =>
                {
                    var pv = r.GetComponentInParent<PhotonView>() ?? r.GetComponent<PhotonView>();
                    return pv != null && pv.IsMine;
                });

            if (myRacer != null)
                StartCoroutine(CoStop(myRacer, duration));
        }

        private IEnumerator CoStop(PlayerRaceData racer, float duration)
        {
            float original = racer.KartSpeed;
            racer.SetKartSpeed(0f);
            yield return new WaitForSeconds(duration);
            if (racer != null) racer.SetKartSpeed(original);
        }
    }
}
