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
    public class HoneyTrapZone : MonoBehaviourPun
    {
        [Header("동작 파라미터")]
        [SerializeField] private float slowMultiplier = 0.5f; 
        [SerializeField] private float slowDuration = 2.0f;

        private bool isTriggered;
        private Collider zoneCol;
        private Renderer[] renderers;

        private class ActiveEffect
        {
            public PlayerRaceData racer;
            public CinemachineDollyCart cart;

            public float originalRacerSpeed; 
            public float originalCartSpeed;  
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
                        var go = new GameObject("HoneyTrapEffectRunner");
                        DontDestroyOnLoad(go);
                        _instance = go.AddComponent<EffectRunner>();
                    }
                    return _instance;
                }
            }

            public ActiveEffect current;

            public void ReplaceWithNew(PlayerRaceData racer, CinemachineDollyCart cart,
                                       float mul, float seconds)
            {
                // 1) 이전 효과 있으면 즉시 원복 + 취소
                if (current != null)
                {
                    current.canceled = true;
                    Restore(current);
                    current = null;
                }

                if (cart == null) return;

                // 2) 새 효과 구성
                var eff = new ActiveEffect
                {
                    racer = racer,
                    cart = cart,

                    originalRacerSpeed = racer != null ? racer.KartSpeed : -1f,
                    originalCartSpeed = cart.m_Speed,
                    canceled = false
                };

                current = eff;
                StartCoroutine(Co_RunSlow(eff, mul, seconds));
            }

            private IEnumerator Co_RunSlow(ActiveEffect eff, float mul, float seconds)
            {
                var racer = eff.racer;
                var cart = eff.cart;

                if (cart == null) yield break;

                float baseSpeed = racer != null && eff.originalRacerSpeed >= 0f
                    ? eff.originalRacerSpeed
                    : eff.originalCartSpeed;

                // 슬로우 적용 (최소 0 이상)
                SetSpeed(racer, cart, Mathf.Max(0f, baseSpeed * Mathf.Clamp01(mul)));

                float t = 0f;
                while (t < seconds)
                {
                    if (eff.canceled) yield break;
                    t += Time.deltaTime;
                    yield return null;
                }

                // 복구 (현효과가 유효할 때만)
                if (!eff.canceled && ReferenceEquals(current, eff))
                {
                    Restore(eff);
                    current = null;
                }
            }

            private void Restore(ActiveEffect eff)
            {
                if (eff == null) return;

                if (eff.cart != null)
                {
                    if (eff.racer != null && eff.originalRacerSpeed >= 0f)
                        eff.racer.SetKartSpeed(eff.originalRacerSpeed);
                    else
                        eff.cart.m_Speed = eff.originalCartSpeed;
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

            var data = photonView?.InstantiationData;
            if (data != null && data.Length >= 2)
            {
                slowMultiplier = (float)data[0];
                slowDuration = (float)data[1];
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!PhotonNetwork.IsMasterClient || isTriggered) return;

            var targetPv = other.GetComponentInParent<PhotonView>();
            if (targetPv == null || targetPv.Owner == null) return;

            var shield = targetPv.GetComponent<PlayerShield>()
                        ?? targetPv.GetComponentInChildren<PlayerShield>(true);
            if (shield != null && shield.IsShieldActive)
            {
                targetPv.RPC(nameof(PlayerShield.RpcActivateShield), targetPv.Owner, 0f); 
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
                Mathf.Clamp01(slowMultiplier), slowDuration);

            float total = slowDuration + 0.2f;
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
        private void RpcApplyTrapReplaceOld(float mul, float seconds)
        {
            // 로컬 소유자 측 실드 재확인
            var localPv = FindObjectsOfType<PhotonView>(true)
                .FirstOrDefault(pv => pv.IsMine);
            var shield = localPv ? (localPv.GetComponent<PlayerShield>() ?? localPv.GetComponentInChildren<PlayerShield>(true)) : null;
            if (shield != null && shield.IsShieldActive)
            {
                shield.SuccessShield(consume: true); // 로컬 즉시 소비
                return;
            }

            var raceData = FindLocalRaceData();
            var cart = FindLocalCart();
            if (cart == null) return;

            EffectRunner.Instance.ReplaceWithNew(raceData, cart, mul, seconds);
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
