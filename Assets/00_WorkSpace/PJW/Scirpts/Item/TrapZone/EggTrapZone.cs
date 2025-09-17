using System.Collections;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    [RequireComponent(typeof(PhotonView))]
    public class EggTrapZone : MonoBehaviourPun
    {
        [Header("동작 파라미터")]
        [SerializeField] private float boostMultiplier = 2f;
        [SerializeField] private float boostTime = 1f;
        [SerializeField] private float waitAfterBoost = 1f;
        [SerializeField] private float stopDuration = 1.5f;

        private bool triggered;
        private Collider zoneCol;
        private Renderer[] renderers;

        private void Awake()
        {
            zoneCol = GetComponent<Collider>();
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!PhotonNetwork.IsMasterClient || triggered) return;

            var targetPv = other.GetComponentInParent<PhotonView>();
            if (targetPv == null) return;

            triggered = true;
            if (zoneCol) zoneCol.enabled = false;
            HideVisuals();

            photonView.RPC(nameof(RpcApplyEggTrapEffect), targetPv.Owner,
                boostMultiplier, boostTime, waitAfterBoost, stopDuration);

            PhotonNetwork.Destroy(gameObject);
        }

        private void HideVisuals()
        {
            if (renderers == null) return;
            foreach (var r in renderers)
                if (r) r.enabled = false;
        }

        [PunRPC]
        private void RpcApplyEggTrapEffect(float boostMult, float boostT, float waitT, float stopT)
        {
            var myRacer = FindObjectsOfType<PlayerRaceData>(true)
                .FirstOrDefault(r =>
                {
                    var pv = r.GetComponentInParent<PhotonView>() ?? r.GetComponent<PhotonView>();
                    return pv != null && pv.IsMine;
                });

            if (myRacer != null)
                StartCoroutine(BoostThenPinStop(myRacer, boostMult, boostT, waitT, stopT));
        }

        private IEnumerator BoostThenPinStop(PlayerRaceData racer, float boostMult, float boostT, float waitT, float stopT)
        {
            float originalSpeed = racer.KartSpeed;

            // 가속
            racer.SetKartSpeed(Mathf.Max(0.1f, originalSpeed * boostMult));
            yield return new WaitForSeconds(boostT);

            // 평속
            racer.SetKartSpeed(originalSpeed);
            yield return new WaitForSeconds(waitT);

            // 정지
            Vector3 pinPos = racer.transform.position;
            Quaternion pinRot = racer.transform.rotation;

            var cart = racer.GetComponent<Cinemachine.CinemachineDollyCart>();
            if (cart != null) cart.enabled = false;

            var rb = racer.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            float t = 0f;
            while (t < stopT)
            {
                if (racer != null)
                    racer.transform.SetPositionAndRotation(pinPos, pinRot);
                yield return null;
                t += Time.unscaledDeltaTime;
            }

            if (cart != null) cart.enabled = true;
            if (racer != null) racer.SetKartSpeed(originalSpeed);
        }
    }
}
