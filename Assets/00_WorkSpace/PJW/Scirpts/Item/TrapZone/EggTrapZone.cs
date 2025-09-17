using System.Collections;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using Cinemachine;

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

        private bool isTriggered;
        private Collider zoneCol;
        private Renderer[] renderers;

        // -------- 런타임 로컬 효과 상태/러너 --------
        private class ActiveEffect
        {
            public PlayerRaceData racer;
            public CinemachineDollyCart cart;
            public Rigidbody rb;

            public float originalRacerSpeed;  
            public float originalCartSpeed;   
            public bool cartWasEnabled;

            public bool rbHad;
            public bool rbWasKinematic;

            public bool canceled;        
        }

        private class EffectRunner : MonoBehaviour
        {
            private static EffectRunner _instance;
            public static EffectRunner Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        var go = new GameObject("EggTrapEffectRunner");
                        DontDestroyOnLoad(go);
                        _instance = go.AddComponent<EffectRunner>();
                    }
                    return _instance;
                }
            }

            public ActiveEffect current; // 로컬 클라에서 단 하나만 유지

            public void ReplaceWithNew(PlayerRaceData racer, CinemachineDollyCart cart,
                                       float mul, float boostSec, float waitSec, float stopSec, float minSpd)
            {
                // 1) 이전 효과 있으면 즉시 원복 + 취소 플래그
                if (current != null)
                {
                    current.canceled = true;
                    Restore(current);
                    current = null;
                }

                if (cart == null) return;

                // 2) 새 효과 구성
                var rb = cart.GetComponent<Rigidbody>();
                var eff = new ActiveEffect
                {
                    racer = racer,
                    cart = cart,
                    rb = rb,

                    originalRacerSpeed = racer != null ? racer.KartSpeed : -1f,
                    originalCartSpeed = cart.m_Speed,
                    cartWasEnabled = cart.enabled,

                    rbHad = rb != null,
                    rbWasKinematic = rb != null ? rb.isKinematic : false,

                    canceled = false
                };

                current = eff;
                StartCoroutine(Co_RunEffect(eff, mul, boostSec, waitSec, stopSec, minSpd));
            }

            private IEnumerator Co_RunEffect(ActiveEffect eff,
                                             float mul, float boostSec, float waitSec, float stopSec, float minSpd)
            {
                var racer = eff.racer;
                var cart = eff.cart;
                var rb = eff.rb;

                if (cart == null) yield break;

                float baseSpeed = racer != null && eff.originalRacerSpeed >= 0f
                    ? eff.originalRacerSpeed
                    : eff.originalCartSpeed;

                // 1) 부스트
                SetSpeed(racer, cart, Mathf.Max(minSpd, baseSpeed * Mathf.Max(0f, mul)));
                float t = 0f;
                while (t < boostSec)
                {
                    if (eff.canceled) yield break;
                    t += Time.deltaTime;
                    yield return null;
                }

                // 2) 미끄러짐
                SetSpeed(racer, cart, baseSpeed);
                t = 0f;
                while (t < waitSec)
                {
                    if (eff.canceled) yield break;
                    t += Time.deltaTime;
                    yield return null;
                }

                // 3) 핀 고정
                Vector3 pinPos = cart.transform.position;
                Quaternion pinRot = cart.transform.rotation;

                SetSpeed(racer, cart, 0f);
                cart.enabled = false;

                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }

                t = 0f;
                while (t < stopSec)
                {
                    if (eff.canceled) { yield break; }
                    cart.transform.SetPositionAndRotation(pinPos, pinRot);
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }

                // 4) 복구
                if (!eff.canceled && ReferenceEquals(current, eff))
                {
                    cart.enabled = eff.cartWasEnabled;
                    if (racer != null && eff.originalRacerSpeed >= 0f)
                        racer.SetKartSpeed(eff.originalRacerSpeed);
                    else
                        cart.m_Speed = eff.originalCartSpeed;

                    if (rb != null)
                        rb.isKinematic = eff.rbWasKinematic;

                    current = null;
                }
            }

            private void Restore(ActiveEffect eff)
            {
                if (eff == null) return;

                if (eff.cart != null)
                {
                    eff.cart.enabled = eff.cartWasEnabled;
                    if (eff.racer != null && eff.originalRacerSpeed >= 0f)
                        eff.racer.SetKartSpeed(eff.originalRacerSpeed);
                    else
                        eff.cart.m_Speed = eff.originalCartSpeed;
                }

                if (eff.rbHad && eff.rb != null)
                {
                    eff.rb.isKinematic = eff.rbWasKinematic;
                    eff.rb.velocity = Vector3.zero;
                    eff.rb.angularVelocity = Vector3.zero;
                }
            }

            private void SetSpeed(PlayerRaceData racer, CinemachineDollyCart cart, float speed)
            {
                if (racer != null) racer.SetKartSpeed(speed);
                else cart.m_Speed = speed;
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

            // 실드 우선 체크
            var shield = targetPv.GetComponent<PlayerShield>()
                         ?? targetPv.GetComponentInChildren<PlayerShield>(true);

            if (shield != null && shield.IsShieldActive)
            {
                // 실드 소비
                targetPv.RPC(nameof(PlayerShield.RpcConsumeShield), targetPv.Owner);

                isTriggered = true;
                photonView.RPC(nameof(RpcHideAndDisable), RpcTarget.All);
                photonView.RPC(nameof(RpcDestroySelfDelayed), RpcTarget.AllBuffered, 0.1f);
                return; 
            }

            // ----- 실드가 없거나 꺼져 있으면 기존 로직 수행 -----
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
        private void RpcApplyTrapReplaceOld(float mul, float boostSec, float waitSec, float stopSec, float minSpd)
        {
            var localPv = FindObjectOfType<PhotonView>(); 
            var shield = localPv ? (localPv.GetComponent<PlayerShield>() ?? localPv.GetComponentInChildren<PlayerShield>(true)) : null;

            if (shield != null && shield.IsShieldActive)
            {
                shield.SuccessShield(consume: true); // 로컬 즉시 소비
                return;
            }

            var raceData = FindLocalRaceData();
            var cart = FindLocalCart();
            if (cart == null) return;

            EffectRunner.Instance.ReplaceWithNew(raceData, cart, mul, boostSec, waitSec, stopSec, minSpd);
        }

        // ---- 유틸: 로컬 소유 플레이어의 컴포넌트 찾기 ----
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
