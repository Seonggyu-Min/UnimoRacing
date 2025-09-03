using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;
using Cinemachine;

namespace PJW
{
    public class RocketProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback, IOnEventCallback
    {
        [Header("유도 설정")]
        [SerializeField] private float speed;
        [SerializeField] private float turnRate;
        [SerializeField] private float maxLifeTime;

        [Header("충돌 반경")]
        [SerializeField] private float hitRadius;

        private int targetViewId;
        private int targetActor;
        private float stunDuration;
        private PhotonView targetPv;
        private float lifeTimer;
        private bool hasHit;

        private const byte RocketStunEvent = 41;

        private sealed class StunRunner : MonoBehaviour
        {
            private static StunRunner _instance;
            private readonly Dictionary<int, Coroutine> running = new Dictionary<int, Coroutine>();

            public static StunRunner Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        var go = new GameObject("[RocketStunRunner]");
                        DontDestroyOnLoad(go);
                        _instance = go.AddComponent<StunRunner>();
                    }
                    return _instance;
                }
            }

            public void ApplyStun(int actorNumber, CinemachineDollyCart cart, float duration)
            {
                if (cart == null || duration <= 0f) return;

                if (running.TryGetValue(actorNumber, out var co) && co != null)
                {
                    StopCoroutine(co);
                }
                running[actorNumber] = StartCoroutine(StunRoutine(actorNumber, cart, duration));
            }

            private IEnumerator StunRoutine(int actorNumber, CinemachineDollyCart cart, float duration)
            {
                float originalSpeed = cart.m_Speed;
                cart.m_Speed = 0f;            
                yield return new WaitForSecondsRealtime(duration);
                if (cart != null)
                {
                    cart.m_Speed = originalSpeed;
                }
                running.Remove(actorNumber);
            }
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            var data = info.photonView.InstantiationData;
            if (data != null && data.Length >= 2)
            {
                targetViewId = (int)data[0];
                stunDuration = (float)data[1];
                targetPv = PhotonView.Find(targetViewId);
                if (targetPv != null && targetPv.Owner != null)
                    targetActor = targetPv.OwnerActorNr;
            }
        }

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        private void OnEnable() { PhotonNetwork.AddCallbackTarget(this); }
        private void OnDisable() { PhotonNetwork.RemoveCallbackTarget(this); }

        private void Update()
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer > maxLifeTime)
            {
                if (photonView.IsMine) PhotonNetwork.Destroy(gameObject);
                return;
            }

            if (!photonView.IsMine) return;
            if (targetPv == null || targetPv.transform == null)
            {
                PhotonNetwork.Destroy(gameObject);
                return;
            }

            Vector3 targetPos = targetPv.transform.position;
            Vector3 dir = (targetPos - transform.position).normalized;

            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnRate * Time.deltaTime);
            }
            transform.position += transform.forward * speed * Time.deltaTime;

            if (!hasHit && Vector3.SqrMagnitude(transform.position - targetPos) <= hitRadius * hitRadius)
            {
                HandleHit(targetPv);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!photonView.IsMine || hasHit) return;

            var hitPv = other.GetComponentInParent<PhotonView>();
            if (hitPv == null) return;
            if (hitPv.ViewID != targetViewId) return;

            HandleHit(hitPv);
        }

        private void HandleHit(PhotonView hitPv)
        {
            hasHit = true;

            object[] content = new object[] { hitPv.OwnerActorNr, stunDuration };
            RaiseEventOptions reo = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            SendOptions so = new SendOptions { Reliability = true };
            PhotonNetwork.RaiseEvent(RocketStunEvent, content, reo, so);

            PhotonNetwork.Destroy(gameObject);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != RocketStunEvent) return;

            var data = photonEvent.CustomData as object[];
            if (data == null || data.Length < 2) return;

            int targetActorNumber = (int)data[0];
            float duration = (float)data[1];

            var me = PhotonNetwork.LocalPlayer;
            if (me == null || me.ActorNumber != targetActorNumber) return;

            var cart = FindMyDollyCart();
            if (cart == null) return;

            StunRunner.Instance.ApplyStun(targetActorNumber, cart, duration);
        }

        private CinemachineDollyCart FindMyDollyCart()
        {
            var pvs = FindObjectsOfType<PhotonView>();
            foreach (var pv in pvs)
            {
                if (pv != null && pv.IsMine)
                {
                    var cart = pv.GetComponentInChildren<CinemachineDollyCart>(true);
                    if (cart != null) return cart;
                }
            }
            return null;
        }
    }
}
