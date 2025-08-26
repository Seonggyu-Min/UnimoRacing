using System.Collections;
using Photon.Pun;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;
using Cinemachine;

namespace PJW
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class RocketProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback, IOnEventCallback
    {
        [Header("유도 설정")]
        [SerializeField] private float speed = 18f;
        [SerializeField] private float turnRate = 720f; 
        [SerializeField] private float maxLifeTime = 8f;
        [Header("충돌 반경(추가 판정 여유)")]
        [SerializeField] private float hitRadius = 0.5f;

        private int targetViewId;
        private int targetActor;
        private float stunDuration;
        private PhotonView targetPv;
        private float lifeTimer;
        private bool hasHit;

        private const byte RocketStunEvent = 41;

        private static bool s_IsStunning;

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
            if (s_IsStunning) return;

            var myRoot = FindMyPlayerRoot();
            if (myRoot == null) return;

            var cart = myRoot.GetComponentInChildren<CinemachineDollyCart>(true);
            if (cart == null) return;

            StartCoroutine(StopCartFor(cart, duration));
        }

        private GameObject FindMyPlayerRoot()
        {
            var pvs = FindObjectsOfType<PhotonView>();
            foreach (var pv in pvs)
            {
                if (pv != null && pv.IsMine)
                    return pv.gameObject;
            }
            return null;
        }

        private IEnumerator StopCartFor(CinemachineDollyCart cart, float duration)
        {
            s_IsStunning = true;

            float originalSpeed = cart.m_Speed;
            cart.m_Speed = 0f;
            cart.enabled = false;

            yield return new WaitForSeconds(duration);

            cart.enabled = true;
            cart.m_Speed = originalSpeed;

            s_IsStunning = false;
        }
    }
}
