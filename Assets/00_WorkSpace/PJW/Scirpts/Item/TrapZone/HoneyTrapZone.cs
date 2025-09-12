using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class HoneyTrapZone : MonoBehaviourPun, IPunInstantiateMagicCallback
    {
        [Header("기본값")]
        [SerializeField] private float slowMultiplier = 0.5f;  
        [SerializeField] private float duration = 2.0f;

        private bool isTriggered;
        private Collider col;
        private Renderer[] rends;

        private void Awake()
        {
            col = GetComponent<Collider>();
            rends = GetComponentsInChildren<Renderer>(true);
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            var data = photonView.InstantiationData;
            if (data != null && data.Length >= 2)
            {
                if (data[0] is float sm) slowMultiplier = sm;
                if (data[1] is float du) duration = du;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isTriggered) return;

            // 실드면 무효화
            var shield = other.GetComponentInParent<PlayerShield>();
            if (shield != null && shield.SuccessShield())
            {
                isTriggered = true;
                photonView.RPC(nameof(RpcHideOnly), RpcTarget.All);
                if (photonView.IsMine || PhotonNetwork.IsMasterClient)
                    StartCoroutine(DestroyAfter(0.05f));
                return;
            }

            var victimPv = other.GetComponentInParent<PhotonView>();
            if (victimPv == null) return;

            isTriggered = true;

            photonView.RPC(nameof(RpcHideOnly), RpcTarget.All);

            photonView.RPC(nameof(RpcApplySlowLocal), RpcTarget.All, victimPv.OwnerActorNr, slowMultiplier, duration);

            if (PhotonNetwork.IsMasterClient)
                StartCoroutine(DestroyAfter(duration + 0.2f)); 
        }

        [PunRPC]
        private void RpcHideOnly()
        {
            if (col) col.enabled = false;
            if (rends != null)
            {
                foreach (var r in rends)
                    if (r) r.enabled = false;
            }
        }

        [PunRPC]
        private void RpcApplySlowLocal(int targetActor, float mul, float dur)
        {
            if (PhotonNetwork.LocalPlayer == null ||
                PhotonNetwork.LocalPlayer.ActorNumber != targetActor) return;

            if (mul <= 0f) return;

            PlayerRaceData myRace = null;
            var all = FindObjectsOfType<PlayerRaceData>(true);
            foreach (var r in all)
            {
                var pv = r.GetComponentInParent<PhotonView>() ?? r.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine) { myRace = r; break; }
            }

            if (myRace == null)
            {
                return;
            }

            StartCoroutine(SlowRoutineRacer(myRace, mul, dur));
        }

        private IEnumerator SlowRoutineRacer(PlayerRaceData data, float mul, float dur)
        {
            float original = data.KartSpeed;
            float slowed = original * mul;

            data.SetKartSpeed(slowed);

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (!Mathf.Approximately(data.KartSpeed, slowed))
                    data.SetKartSpeed(slowed);
                yield return null;
            }

            if (Mathf.Approximately(data.KartSpeed, slowed))
                data.SetKartSpeed(original);
        }

        private IEnumerator DestroyAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (this != null && photonView != null && photonView.ViewID != 0)
            {
                if (PhotonNetwork.IsMasterClient || photonView.IsMine)
                    PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}
