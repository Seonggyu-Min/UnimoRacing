using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YTW;

namespace PJW
{
    public class RocketProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback, IOnEventCallback, IPunObservable
    {
        [Header("유도 설정")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float turnRate = 360f;
        [SerializeField] private float maxLifeTime = 8f;

        [Header("충돌 반경")]
        [SerializeField] private float hitRadius = 2f;

        private int targetViewId;
        private int targetActor;
        private float stunDuration;
        private PhotonView targetPv;
        private float lifeTimer;
        private bool hasHit;

        // 네트워크 보간용
        private Vector3 networkPos;
        private Quaternion networkRot;
        private bool hasNetSnapshot;
        [SerializeField] private float netLerp = 20f;

        [SerializeField] private string sfxHitKey = "Bang";

        [SerializeField] private string sfxFlyLoopKey = "Missile_Coming";  
        private AudioSource flyLoopSource;

        private const byte RocketStunEvent = 41;

        // 로컬 구동 권한: 소유자 or 마스터 (소유권 꼬여도 마스터가 백업으로 구동)
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

            // 자신을 ObservedComponents에 강제 등록
            if (photonView != null)
            {
                if (photonView.ObservedComponents == null)
                    photonView.ObservedComponents = new List<Component>();
                if (!photonView.ObservedComponents.Contains(this))
                    photonView.ObservedComponents.Add(this);

                photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
            }
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);

            if (!string.IsNullOrEmpty(sfxFlyLoopKey))
            {
                if (flyLoopSource == null)
                {
                    flyLoopSource = gameObject.AddComponent<AudioSource>();
                    flyLoopSource.playOnAwake = false;
                    flyLoopSource.spatialBlend = 1f; // 3D
                }
                AudioManager.Instance.PlayLoopingSoundOn(flyLoopSource, sfxFlyLoopKey, 0.05f);
            }
        }
        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);

            // 비행 루프 사운드 정지(페이드아웃 후 정지)
            if (flyLoopSource != null)
            {
                AudioManager.Instance.StopSoundOn(flyLoopSource, 0.1f);
            }
        }

        private void Update()
        {
            // 수명
            lifeTimer += Time.deltaTime;
            if (lifeTimer > maxLifeTime)
            {
                if (photonView != null && photonView.IsMine)
                    PhotonNetwork.Destroy(gameObject);
                return;
            }

            // 구동(이동/유도): 소유자 or 마스터
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

                // 명중 판정은 한쪽만 수행 (마스터가 우선, 아니면 소유자)
                bool hasAuthorityForHit = PhotonNetwork.IsMasterClient || (photonView != null && photonView.IsMine);
                if (hasAuthorityForHit && !hasHit &&
                    (transform.position - targetPos).sqrMagnitude <= hitRadius * hitRadius)
                {
                    HandleHit(targetPv);
                }
            }
            else
            {
                // 비구동 클라: 네트워크 스냅샷 보간
                if (hasNetSnapshot)
                {
                    transform.position = Vector3.Lerp(transform.position, networkPos, netLerp * Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, networkRot, netLerp * Time.deltaTime);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 트리거 충돌도 권한 측만 처리
            if (!CanDrive || hasHit) return;

            var hitPv = other.GetComponentInParent<PhotonView>();
            if (hitPv == null || hitPv.ViewID != targetViewId) return;

            HandleHit(hitPv);
        }

        private void HandleHit(PhotonView hitPv)
        {
            hasHit = true;

            AudioManager.Instance.PlaySFX(sfxHitKey);

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

                // 내 로컬 플레이어의 PlayerRaceData 찾기
                var racer = FindObjectsOfType<PlayerRaceData>(true)
                    .FirstOrDefault(r =>
                    {
                        var pv = r.GetComponentInParent<PhotonView>() ?? r.GetComponent<PhotonView>();
                        return pv != null && pv.IsMine;
                    });

                if (racer == null) return;

                // PlayerRaceData 기반으로 스턴 적용
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

        // 위치/회전 직렬화
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
