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

        [Header("사운드 키")]
        [SerializeField] private string sfxHitKey = "Bang";
        [SerializeField] private string sfxFlyLoopKey = "Missile_Coming_SFX";

        [Header("타겟 경고 사운드(타겟 클라 전용)")]
        [SerializeField] private string sfxIncomingKey = "Missile_Coming_SFX"; 
        [SerializeField] private float warnMaxDistance = 40f;
        [SerializeField] private float warnMinPitch = 0.9f;
        [SerializeField] private float warnMaxPitch = 1.6f;
        [SerializeField] private float warnSmoothing = 10f;

        private AudioSource flyLoopSource;
        private AudioSource incomingWarnSource;  // 타겟 전용 2D 오디오
        private bool isMyTarget;                 // 내 캐릭터가 타겟인지
        private float warnVol;                   // 보간 볼륨
        private float warnPitch;                 // 보간 피치

        private const byte RocketStunEvent = 41;

        // 로컬 구동 권한
        private bool CanDrive => photonView != null && (photonView.IsMine || PhotonNetwork.IsMasterClient);

        // ------------------- 스턴 러너 -------------------
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

            private const float StopFactor = 1e-6f;
            private readonly Dictionary<int, int> stunStackByActor = new Dictionary<int, int>();
            private readonly Dictionary<int, PlayerRaceData> racerByActor = new Dictionary<int, PlayerRaceData>();

            public void ApplyStun(int actorNumber, PlayerRaceData racer, float duration)
            {
                if (racer == null || duration <= 0f) return;

                racerByActor[actorNumber] = racer;

                if (!stunStackByActor.TryGetValue(actorNumber, out var stack))
                    stack = 0;

                if (stack == 0)
                    ApplyStopFactor(racer);

                stunStackByActor[actorNumber] = stack + 1;

                StartCoroutine(StunTimer(actorNumber, duration));
            }

            private IEnumerator StunTimer(int actorNumber, float duration)
            {
                yield return new WaitForSecondsRealtime(duration);

                if (!stunStackByActor.TryGetValue(actorNumber, out var stack))
                    yield break;

                stack -= 1;
                if (stack <= 0)
                {
                    stunStackByActor.Remove(actorNumber);
                    if (racerByActor.TryGetValue(actorNumber, out var racer) && racer != null)
                        RemoveStopFactor(racer);

                    racerByActor.Remove(actorNumber);
                }
                else
                {
                    stunStackByActor[actorNumber] = stack;
                }
            }

            private void ApplyStopFactor(PlayerRaceData racer)
            {
                var v = racer.KartSpeed;
                racer.SetKartSpeed(v * StopFactor);
            }

            private void RemoveStopFactor(PlayerRaceData racer)
            {
                var v = racer.KartSpeed;
                racer.SetKartSpeed(v / StopFactor);
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

            // 내가 타겟인지 판정
            isMyTarget = (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == targetActor);
        }

        private void Awake()
        {
            var col = GetComponent<Collider>(); if (col) col.isTrigger = true;
            var rb = GetComponent<Rigidbody>(); if (rb) rb.isKinematic = true;

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

            // 미사일 비행 사운드
            if (!string.IsNullOrEmpty(sfxFlyLoopKey))
            {
                if (flyLoopSource == null)
                {
                    flyLoopSource = gameObject.AddComponent<AudioSource>();
                    flyLoopSource.playOnAwake = false;
                    flyLoopSource.spatialBlend = 1f;
                }
                AudioManager.Instance.PlayLoopingSoundOn(flyLoopSource, sfxFlyLoopKey, 0.05f);
            }

            if (isMyTarget && !string.IsNullOrEmpty(sfxIncomingKey))
            {
                if (incomingWarnSource == null)
                {
                    incomingWarnSource = gameObject.AddComponent<AudioSource>();
                    incomingWarnSource.playOnAwake = false;
                    incomingWarnSource.spatialBlend = 0f; // 2D
                    incomingWarnSource.loop = true;
                }
                AudioManager.Instance.PlayLoopingSoundOn(incomingWarnSource, sfxIncomingKey, 0f);
                warnVol = 0f;
                warnPitch = warnMinPitch;
            }
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);

            if (flyLoopSource != null)
                AudioManager.Instance.StopSoundOn(flyLoopSource, 0.1f);

            if (incomingWarnSource != null)
                AudioManager.Instance.StopSoundOn(incomingWarnSource, 0.1f);
        }

        private void Update()
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer > maxLifeTime)
            {
                if (photonView != null && photonView.IsMine)
                    PhotonNetwork.Destroy(gameObject);
                return;
            }

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

                bool hasAuthorityForHit = PhotonNetwork.IsMasterClient || (photonView != null && photonView.IsMine);
                if (hasAuthorityForHit && !hasHit &&
                    (transform.position - targetPos).sqrMagnitude <= hitRadius * hitRadius)
                {
                    HandleHit(targetPv);
                }
            }
            else
            {
                if (hasNetSnapshot)
                {
                    transform.position = Vector3.Lerp(transform.position, networkPos, netLerp * Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, networkRot, netLerp * Time.deltaTime);
                }
            }

            if (isMyTarget && incomingWarnSource != null)
            {
                var myPv = FindObjectsOfType<PhotonView>().FirstOrDefault(p => p != null && p.IsMine);
                Transform myTf = myPv ? myPv.transform : null;

                if (myTf != null)
                {
                    float dist = Vector3.Distance(transform.position, myTf.position);
                    float t = Mathf.Clamp01(1f - (dist / Mathf.Max(0.001f, warnMaxDistance)));

                    warnVol = Mathf.Lerp(warnVol, t, warnSmoothing * Time.deltaTime);
                    warnPitch = Mathf.Lerp(warnPitch, Mathf.Lerp(warnMinPitch, warnMaxPitch, t), warnSmoothing * Time.deltaTime);

                    incomingWarnSource.volume = warnVol;
                    incomingWarnSource.pitch = warnPitch;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
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

                var racer = FindObjectsOfType<PlayerRaceData>(true)
                    .FirstOrDefault(r =>
                    {
                        var pv = r.GetComponentInParent<PhotonView>() ?? r.GetComponent<PhotonView>();
                        return pv != null && pv.IsMine;
                    });

                if (racer == null) return;
                StunRunner.Instance.ApplyStun(targetActorNumber, racer, duration);
            }
        }

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
