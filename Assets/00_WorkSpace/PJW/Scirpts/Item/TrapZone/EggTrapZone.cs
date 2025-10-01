using Cinemachine;
using Photon.Pun;
using System.Collections;
using UnityEngine;
using YTW;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(PhotonView))]
    public class EggTrapZone : MonoBehaviourPun
    {
        [Header("동작 파라미터")]
        [SerializeField] private float boostMultiplier = 2f;
        [SerializeField] private float boostTime = 1f;
        [SerializeField] private float waitAfterBoost = 1f;
        [SerializeField] private float stopDuration = 1.5f;

        [Header("최소 유효 속도")]
        [SerializeField] private float minSpeed = 0.1f;

        [SerializeField] private string sfxUseKey = "Egg_Crash";

        private bool isTriggered;
        private Collider zoneCol;
        private Renderer[] renderers;

        private class ActiveEffect
        {
            public PlayerRaceData racer;
            public CinemachineDollyCart cart;
            public Rigidbody rb;

            public bool rbHad;
            public bool rbWasKinematic;

            public float appliedMul;
            public bool canceled;

            // CHANGED: 아이템 적용 '이전' 속도 스냅샷
            public float originalSpeedSnapshot;
        }

        private class EffectRunner : MonoBehaviour
        {
            private static EffectRunner instance;
            public static EffectRunner Instance
            {
                get
                {
                    if (instance == null)
                    {
                        var go = new GameObject("EggTrapEffectRunner");
                        Object.DontDestroyOnLoad(go);
                        instance = go.AddComponent<EffectRunner>();
                    }
                    return instance;
                }
            }

            public ActiveEffect current;

            public void ReplaceWithNew(PlayerRaceData racer, CinemachineDollyCart cart,
                                       float mul, float boostSec, float waitSec, float stopSec, float minSpd)
            {
                if (current != null)
                {
                    current.canceled = true;
                    Restore(current);
                    current = null;
                }

                if (cart == null) return;

                var rb = cart.GetComponent<Rigidbody>();
                var eff = new ActiveEffect
                {
                    racer = racer,
                    cart = cart,
                    rb = rb,
                    rbHad = rb != null,
                    rbWasKinematic = rb != null ? rb.isKinematic : false,
                    appliedMul = 0f,
                    canceled = false,
                    originalSpeedSnapshot = 0f
                };

                current = eff;
                StartCoroutine(CoRunEffect(eff, mul, boostSec, waitSec, stopSec, minSpd));
            }

            private IEnumerator CoRunEffect(ActiveEffect eff,
                                            float mul, float boostSec, float waitSec, float stopSec, float minSpd)
            {
                var racer = eff.racer;
                var cart = eff.cart;
                var rb = eff.rb;
                if (cart == null) yield break;

                // CHANGED: 아이템 적용 '이전' 속도 스냅샷
                eff.originalSpeedSnapshot = (racer != null) ? racer.KartSpeed : cart.m_Speed;

                // ==== (A) BOOST — 곱 적용 ====
                float applyMul = Mathf.Max(0.0001f, mul);

                if (racer != null)
                {
                    float after = racer.KartSpeed * applyMul;
                    if (after < minSpd && racer.KartSpeed > 0f)
                        applyMul = minSpd / Mathf.Max(0.0001f, racer.KartSpeed);

                    racer.SetKartSpeed(racer.KartSpeed * applyMul);
                }
                else
                {
                    float after = cart.m_Speed * applyMul;
                    if (after < minSpd && cart.m_Speed > 0f)
                        applyMul = minSpd / Mathf.Max(0.0001f, cart.m_Speed);

                    cart.m_Speed = cart.m_Speed * applyMul;
                }

                eff.appliedMul = applyMul;

                float t = 0f;
                while (t < boostSec)
                {
                    if (eff.canceled) { Restore(eff); yield break; }
                    t += Time.deltaTime;
                    yield return null;
                }

                // ==== (B) WAIT ====
                t = 0f;
                while (t < waitSec)
                {
                    if (eff.canceled) { Restore(eff); yield break; }
                    t += Time.deltaTime;
                    yield return null;
                }

                // ==== (C) PIN ====
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }

                t = 0f;
                while (t < stopSec)
                {
                    if (eff.canceled) { Restore(eff); yield break; }

                    if (racer != null) racer.SetKartSpeed(0f);
                    else cart.m_Speed = 0f;

                    t += Time.deltaTime;
                    yield return null;
                }

                // ==== (D) 복구 ====
                if (!eff.canceled && ReferenceEquals(current, eff))
                {
                    if (rb != null)
                        rb.isKinematic = eff.rbWasKinematic;

                    // 1) 우리 배수만 제거
                    if (eff.appliedMul > 0f)
                    {
                        if (racer != null) racer.SetKartSpeed(racer.KartSpeed / eff.appliedMul);
                        else cart.m_Speed = cart.m_Speed / eff.appliedMul;
                        eff.appliedMul = 0f;
                    }

                    // 2) 아직도 정지 상태(≈0)라면 '기존 속도'로만 복귀
                    const float EPS = 0.0001f;
                    if (racer != null)
                    {
                        if (Mathf.Abs(racer.KartSpeed) <= EPS)
                            racer.SetKartSpeed(eff.originalSpeedSnapshot); // CHANGED
                    }
                    else
                    {
                        if (Mathf.Abs(cart.m_Speed) <= EPS)
                            cart.m_Speed = eff.originalSpeedSnapshot;      // CHANGED
                    }

                    current = null;
                }
            }

            private void Restore(ActiveEffect eff)
            {
                if (eff == null) return;

                if (eff.rbHad && eff.rb != null)
                {
                    eff.rb.isKinematic = eff.rbWasKinematic;
                    eff.rb.velocity = Vector3.zero;
                    eff.rb.angularVelocity = Vector3.zero;
                }

                if (eff.appliedMul > 0f)
                {
                    if (eff.racer != null) eff.racer.SetKartSpeed(eff.racer.KartSpeed / eff.appliedMul);
                    else eff.cart.m_Speed = eff.cart.m_Speed / eff.appliedMul;
                    eff.appliedMul = 0f;
                }
                // CHANGED: 강제 복귀 없음(다른 아이템 변경을 존중)
            }
        }

        private void Awake()
        {
            zoneCol = GetComponent<Collider>();
            if (zoneCol) zoneCol.isTrigger = true;
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!PhotonNetwork.IsMasterClient || isTriggered) return;

            var targetPv = other.GetComponentInParent<PhotonView>();
            if (targetPv == null || targetPv.Owner == null) return;

            // 실드 우선
            var shield = targetPv.GetComponent<PlayerShield>() ?? targetPv.GetComponentInChildren<PlayerShield>(true);
            if (shield != null && shield.IsShieldActive)
            {
                targetPv.RPC(nameof(PlayerShield.RpcConsumeShield), targetPv.Owner);
                isTriggered = true;
                photonView.RPC(nameof(RpcHideAndDisable), RpcTarget.All);
                photonView.RPC(nameof(RpcDestroySelfDelayed), RpcTarget.AllBuffered, 0.1f);
                return;
            }

            var cart = other.GetComponentInParent<CinemachineDollyCart>();
            if (cart == null) return;

            isTriggered = true;
            photonView.RPC(nameof(RpcHideAndDisable), RpcTarget.All);

            photonView.RPC(nameof(RpcApplyTrapReplaceOld), targetPv.Owner,
                boostMultiplier, boostTime, waitAfterBoost, stopDuration, minSpeed);

            float total = boostTime + waitAfterBoost + stopDuration + 0.2f;
            photonView.RPC(nameof(RpcDestroySelfDelayed), RpcTarget.AllBuffered, total);
        }

        [PunRPC]
        private void RpcHideAndDisable()
        {
            if (!string.IsNullOrEmpty(sfxUseKey))
                AudioManager.Instance?.PlaySFX(sfxUseKey);

            if (zoneCol) zoneCol.enabled = false;
            if (renderers != null)
            {
                foreach (var r in renderers)
                    if (r) r.enabled = false;
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
        private void RpcApplyTrapReplaceOld(float mul, float boostSec, float waitSec, float stopSec, float minSpd)
        {
            var raceData = FindLocalRaceData();
            var cart = FindLocalCart();
            if (cart == null) return;

            var shield = (raceData != null)
                ? (raceData.GetComponentInChildren<PlayerShield>(true))
                : (cart.GetComponentInChildren<PlayerShield>(true));

            if (shield != null && shield.IsShieldActive)
            {
                shield.SuccessShield(consume: true);
                return;
            }

            EffectRunner.Instance.ReplaceWithNew(raceData, cart, mul, boostSec, waitSec, stopSec, minSpd);
        }

        private PlayerRaceData FindLocalRaceData()
        {
            var all = FindObjectsOfType<PlayerRaceData>(true);
            foreach (var rd in all)
            {
                var pv = rd.GetComponentInParent<PhotonView>() ?? rd.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine) return rd;
            }
            return null;
        }

        private CinemachineDollyCart FindLocalCart()
        {
            var all = FindObjectsOfType<CinemachineDollyCart>(true);
            foreach (var c in all)
            {
                var pv = c.GetComponentInParent<PhotonView>() ?? c.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine) return c;
            }
            return null;
        }
    }
}
