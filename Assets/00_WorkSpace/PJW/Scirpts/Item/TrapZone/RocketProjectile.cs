using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PJW
{
    public class RocketProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback, IOnEventCallback, IPunObservable
    {
        [Header("���� ����")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float turnRate = 360f;
        [SerializeField] private float maxLifeTime = 8f;

        [Header("�浹 �ݰ�")]
        [SerializeField] private float hitRadius = 2f;

        private int targetViewId;
        private int targetActor;
        private float stunDuration;
        private PhotonView targetPv;
        private float lifeTimer;
        private bool hasHit;
        private Vector3 netPos;
        private Quaternion netRot;

        // ��Ʈ��ũ ������
        private Vector3 networkPos;
        private Quaternion networkRot;
        private bool hasNetSnapshot;
        [SerializeField] private float netLerp = 20f;

        private const byte RocketStunEvent = 41;

        // ���� ���� ����: ������ or ������ (������ ������ �����Ͱ� ������� ����)
        private bool CanDrive => photonView != null && (photonView.IsMine || PhotonNetwork.IsMasterClient);

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
                        Object.DontDestroyOnLoad(go);
                        _instance = go.AddComponent<StunRunner>();
                    }
                    return _instance;
                }
            }

            public void ApplyStun(int actorNumber, PlayerRaceData racer, float duration)
            {
                if (racer == null || duration <= 0f) return;

                if (running.TryGetValue(actorNumber, out var co) && co != null)
                    StopCoroutine(co);

                running[actorNumber] = StartCoroutine(StunRoutine(actorNumber, racer, duration));
            }

            private IEnumerator StunRoutine(int actorNumber, PlayerRaceData racer, float duration)
            {
                float original = racer.KartSpeed;
                racer.SetKartSpeed(0f);

                yield return new WaitForSecondsRealtime(duration);

                if (racer != null)
                    racer.SetKartSpeed(original);

                running.Remove(actorNumber);
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting) // ���� -> �ٸ� Ŭ��� ����
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
            }
            else // ����� -> ���� ������ ����
            {
                netPos = (Vector3)stream.ReceiveNext();
                netRot = (Quaternion)stream.ReceiveNext();
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

            networkPos = transform.position;
            networkRot = transform.rotation;
            hasNetSnapshot = true;
        }

        private void Awake()
        {
            var col = GetComponent<Collider>(); if (col) col.isTrigger = true;
            var rb = GetComponent<Rigidbody>(); if (rb) rb.isKinematic = true;

            // �ڽ��� ObservedComponents�� ���� ���
            if (photonView != null)
            {
                if (photonView.ObservedComponents == null)
                    photonView.ObservedComponents = new List<Component>();
                if (!photonView.ObservedComponents.Contains(this))
                    photonView.ObservedComponents.Add(this);

                photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
            }
        }

        private void OnEnable() { PhotonNetwork.AddCallbackTarget(this); }
        private void OnDisable() { PhotonNetwork.RemoveCallbackTarget(this); }

        private void Update()
        {
            // ����
            lifeTimer += Time.deltaTime;
            if (lifeTimer > maxLifeTime)
            {
                if (photonView != null && photonView.IsMine)
                    PhotonNetwork.Destroy(gameObject);
                return;
            }

            // ����(�̵�/����): ������ or ������
            if (CanDrive)
            {
                if (targetPv == null || targetPv.transform == null)
                {
                    if (photonView != null && photonView.IsMine)
                        PhotonNetwork.Destroy(gameObject);
                    return;
                }

                Vector3 targetPos = targetPv.transform.position;
                Vector3 dir = (targetPos - transform.position).normalized;

                if (dir.sqrMagnitude > 1e-6f)
                {
                    Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnRate * Time.deltaTime);
                }

                transform.position += transform.forward * speed * Time.deltaTime;

                // ���� ������ ���ʸ� ���� (�����Ͱ� �켱, �ƴϸ� ������)
                bool hasAuthorityForHit = PhotonNetwork.IsMasterClient || (photonView != null && photonView.IsMine);
                if (hasAuthorityForHit && !hasHit &&
                    (transform.position - targetPos).sqrMagnitude <= hitRadius * hitRadius)
                {
                    HandleHit(targetPv);
                }
            }
            else
            {
                // �񱸵� Ŭ��: ��Ʈ��ũ ������ ����
                if (hasNetSnapshot)
                {
                    transform.position = Vector3.Lerp(transform.position, networkPos, netLerp * Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, networkRot, netLerp * Time.deltaTime);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Ʈ���� �浹�� ���� ���� ó��
            if (!CanDrive || hasHit) return;

            var hitPv = other.GetComponentInParent<PhotonView>();
            if (hitPv == null || hitPv.ViewID != targetViewId) return;

            HandleHit(hitPv);
        }

        private void HandleHit(PhotonView hitPv)
        {
            hasHit = true;

            object[] content = new object[] { hitPv.OwnerActorNr, stunDuration };
            RaiseEventOptions reo = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            SendOptions so = new SendOptions { Reliability = true };
            PhotonNetwork.RaiseEvent(RocketStunEvent, content, reo, so);

            if (photonView != null && photonView.IsMine)
                PhotonNetwork.Destroy(gameObject);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != RocketStunEvent) return;

            if (photonEvent.CustomData is object[] data && data.Length >= 2)
            {
                int targetActorNumber = (int)data[0];
                float duration = (float)data[1];

                var me = PhotonNetwork.LocalPlayer;
                if (me == null || me.ActorNumber != targetActorNumber) return;

                // �� ���� �÷��̾��� PlayerRaceData ã��
                var racer = FindObjectsOfType<PlayerRaceData>(true)
                    .FirstOrDefault(r =>
                    {
                        var pv = r.GetComponentInParent<PhotonView>() ?? r.GetComponent<PhotonView>();
                        return pv != null && pv.IsMine;
                    });

                if (racer == null) return;

                // PlayerRaceData ������� ���� ����
                StunRunner.Instance.ApplyStun(targetActorNumber, racer, duration);
            }
        }

        private Cinemachine.CinemachineDollyCart FindMyDollyCart()
        {
            foreach (var pv in FindObjectsOfType<PhotonView>())
            {
                if (pv != null && pv.IsMine)
                {
                    var cart = pv.GetComponentInChildren<Cinemachine.CinemachineDollyCart>(true);
                    if (cart != null) return cart;
                }
            }
            return null;
        }

        // ��ġ/ȸ�� ����ȭ
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
            }
            else
            {
                networkPos = (Vector3)stream.ReceiveNext();
                networkRot = (Quaternion)stream.ReceiveNext();
                hasNetSnapshot = true;
            }
        }
    }
}
