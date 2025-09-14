using Cinemachine;
using Photon.Pun;
using System.Collections;
using UnityEngine;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(PhotonView))]
    public class BigEggTrapZone : MonoBehaviourPun
    {
        [Header("동작 파라미터")]
        [SerializeField] private float boostMultiplier = 2f;
        [SerializeField] private float boostTime = 1f;
        [SerializeField] private float waitAfterBoost = 1f;
        [SerializeField] private float stopDuration = 1.5f;

        [Header("최소 유효 속도")]
        [SerializeField] private float minSpeed = 0.1f;

        private bool isTriggered;
        private Collider zoneCol;
        private Renderer[] renderers;

        private void Awake()
        {
            zoneCol = GetComponent<Collider>();
            if (zoneCol) zoneCol.isTrigger = true;
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isTriggered) return;

            if (!PhotonNetwork.IsMasterClient) return;

            var cart = other.GetComponentInParent<CinemachineDollyCart>();
            if (cart == null) return;

            var targetPv = other.GetComponentInParent<PhotonView>();
            if (targetPv == null || targetPv.Owner == null) return;

            isTriggered = true;

            photonView.RPC(nameof(RpcHideAndDisable), RpcTarget.All);

            photonView.RPC(nameof(RpcApplyTrapToTarget), targetPv.Owner,
                boostMultiplier, boostTime, waitAfterBoost, stopDuration, minSpeed);

            float total = boostTime + waitAfterBoost + stopDuration + 0.2f; 
            photonView.RPC(nameof(RpcDestroySelfDelayed), RpcTarget.AllBuffered, total);
        }

        [PunRPC]
        private void RpcHideAndDisable()
        {
            if (zoneCol) zoneCol.enabled = false;
            if (renderers != null)
            {
                foreach (var r in renderers)
                {
                    if (r) r.enabled = false;
                }
            }
        }

        [PunRPC]
        private void RpcDestroySelfDelayed(float delay)
        {
            if (this == null || gameObject == null) return;
            StartCoroutine(DestroyAfter(delay));
        }

        private IEnumerator DestroyAfter(float delay)
        {
            float t = 0f;
            while (t < delay)
            {
                t += Time.deltaTime;
                yield return null;
            }
            if (this != null && gameObject != null)
                Destroy(gameObject);
        }

        [PunRPC]
        private void RpcApplyTrapToTarget(float mul, float boostSec, float waitSec, float stopSec, float minSpd)
        {
            var raceData = FindLocalRaceData();
            var cart = FindLocalCart();
            if (cart == null) return;

            StartCoroutine(BoostThenPinStopLocal(cart, raceData, mul, boostSec, waitSec, stopSec, minSpd));
        }

        private IEnumerator BoostThenPinStopLocal(CinemachineDollyCart cart, PlayerRaceData raceData,
                                                  float mul, float boostSec, float waitSec, float stopSec, float minSpd)
        {
            float originalRacerSpeed = raceData != null ? raceData.KartSpeed : -1f;
            float originalCartSpeed = cart.m_Speed;
            float baseSpeed = originalRacerSpeed >= 0f ? originalRacerSpeed : originalCartSpeed;

            // 1) 부스트
            float boosted = Mathf.Max(minSpd, baseSpeed * Mathf.Max(0f, mul));
            SetRacerSpeed(raceData, cart, boosted);
            yield return new WaitForSeconds(boostSec);

            // 2) 평속 대기
            SetRacerSpeed(raceData, cart, baseSpeed);
            yield return new WaitForSeconds(waitSec);

            // 3) 핀 정지
            Vector3 pinPos = cart.transform.position;
            Quaternion pinRot = cart.transform.rotation;

            SetRacerSpeed(raceData, cart, 0f); 

            cart.enabled = false;
            var rb = cart.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            float t = 0f;
            while (t < stopSec)
            {
                cart.transform.SetPositionAndRotation(pinPos, pinRot);
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // 4) 복구
            cart.enabled = true;
            if (raceData != null) raceData.SetKartSpeed(originalRacerSpeed);
            else cart.m_Speed = originalCartSpeed;
        }

        private void SetRacerSpeed(PlayerRaceData raceData, CinemachineDollyCart cart, float speed)
        {
            if (raceData != null) raceData.SetKartSpeed(speed);
            else cart.m_Speed = speed;
        }

        private PlayerRaceData FindLocalRaceData()
        {
            var all = FindObjectsOfType<PlayerRaceData>(true);
            foreach (var rd in all)
            {
                var pv = rd.GetComponentInParent<PhotonView>();
                if (pv != null && pv.IsMine) return rd;
            }
            return null;
        }

        private CinemachineDollyCart FindLocalCart()
        {
            var all = FindObjectsOfType<CinemachineDollyCart>(true);
            foreach (var c in all)
            {
                var pv = c.GetComponentInParent<PhotonView>();
                if (pv != null && pv.IsMine) return c;
            }
            return null;
        }
    }
}
