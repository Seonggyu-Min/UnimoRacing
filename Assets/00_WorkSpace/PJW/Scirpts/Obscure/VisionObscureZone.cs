using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;
using YTW;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(PhotonView))]
    public class VisionObscureZone : MonoBehaviourPun
    {
        [Header("오버레이 리소스 이름 (Resources/)")]
        [SerializeField] private string obscureOverlayResource = "ObscureOverlay";

        [Header("효과 시간(초)")]
        [SerializeField] private float duration = 2f;

        [Header("페이드 파라미터")]
        [SerializeField] private float fadeIn = 0.2f;
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.9f;
        [SerializeField] private float fadeOut = 0.4f;

        [Header("트리거 1회만 동작")]
        [SerializeField] private bool oneShot = true;

        [Header("사운드 키")]
        [SerializeField] private string sfxHitKey = "Blind_Hit_SFX";

        [Header("자동 파괴(초) - 0 이하면 비활성")]
        [SerializeField] private float lifetime = 0f;

        [Header("이펙트 오브젝트 (Prefab 또는 Child 1개)")]
        [SerializeField] private GameObject particleObject;
        [Tooltip("true면 particleObject를 프리팹으로 보고 현재 위치에 인스턴스 생성")]
        [SerializeField] private bool instantiateParticle = false;

        private bool triggered;
        private Collider zoneCollider;
        private Renderer[] renderers;
        private GameObject particleRuntime;

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Start()
        {
            // 설치 즉시 이펙트 표시
            if (particleObject != null)
            {
                if (instantiateParticle)
                {
                    particleRuntime = Instantiate(particleObject, transform.position, transform.rotation, transform);
                    ShowEffectObject(particleRuntime, true);
                }
                else
                {
                    ShowEffectObject(particleObject, true);
                }
            }

            // lifetime 자동 파괴는 마스터만 예약
            if (lifetime > 0f && PhotonNetwork.IsMasterClient)
                StartCoroutine(CoDestroyAfter(lifetime));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (oneShot && triggered) return;

            var targetPv = other.GetComponentInParent<PhotonView>();
            if (targetPv == null) return;

            triggered = true;

            // 존 비주얼 닫기
            DisableZoneVisuals();

            // 충돌한 대상의 오너 클라이언트에서만 시야 방해 오버레이 표시
            photonView.RPC(nameof(RpcApplyObscureOnLocal), targetPv.Owner,
                obscureOverlayResource, duration, fadeIn, maxAlpha, fadeOut);

            // 사운드
            if (!string.IsNullOrEmpty(sfxHitKey))
                AudioManager.Instance.PlaySFX(sfxHitKey);

            // 원샷 모드면 충돌 후 잠시 뒤 파괴 (마스터가 책임)
            if (oneShot)
                StartCoroutine(CoDestroyAfter(0.5f));
        }

        private void DisableZoneVisuals()
        {
            if (zoneCollider != null) zoneCollider.enabled = false;

            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) renderers[i].enabled = false;
            }

            var target = particleRuntime != null ? particleRuntime : particleObject;
            if (target != null)
                ShowEffectObject(target, false);
        }

        private void ShowEffectObject(GameObject obj, bool show)
        {
            if (obj == null) return;

            obj.SetActive(true); // Play/Stop 위해 잠시 활성화
            var psList = obj.GetComponentsInChildren<ParticleSystem>(true);
            if (psList != null && psList.Length > 0)
            {
                for (int i = 0; i < psList.Length; i++)
                {
                    if (psList[i] == null) continue;
                    if (show) psList[i].Play(true);
                    else psList[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }

            var vfxList = obj.GetComponentsInChildren<VisualEffect>(true);
            if (vfxList != null && vfxList.Length > 0)
            {
                for (int i = 0; i < vfxList.Length; i++)
                {
                    if (vfxList[i] == null) continue;
                    if (show) vfxList[i].Play();
                    else vfxList[i].Stop();
                }
            }

            if (!show)
                obj.SetActive(false);
        }

        private IEnumerator CoDestroyAfter(float t)
        {
            yield return new WaitForSeconds(t);

            if (photonView != null)
            {
                if (photonView.IsMine)
                {
                    PhotonNetwork.Destroy(photonView);
                    yield break;
                }
                if (PhotonNetwork.IsMasterClient /* && photonView.IsSceneView */)
                {
                    PhotonNetwork.Destroy(photonView);
                    yield break;
                }
                yield break;
            }

            Destroy(gameObject);
        }

        [PunRPC]
        private void RpcApplyObscureOnLocal(string resourceName, float keep, float fIn, float aMax, float fOut)
        {
            var prefab = Resources.Load<GameObject>(resourceName);
            if (prefab == null)
            {
                Debug.LogError($"[VisionObscureZone] Resources '{resourceName}' not found.");
                return;
            }

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("AutoOverlayCanvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
            }

            var overlay = Instantiate(prefab, canvas.transform);
            overlay.transform.SetAsLastSibling();

            var cg = overlay.GetComponent<CanvasGroup>();
            if (cg == null) cg = overlay.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var img = overlay.GetComponentInChildren<Image>(true);
            if (img != null) img.raycastTarget = false;

            overlay.SetActive(true);

            //  코루틴을 오버레이 자신이 담당하도록 위임 (존이 파괴되어도 정상 종료)
            var fader = overlay.GetComponent<OverlayFader>();
            if (fader == null) fader = overlay.AddComponent<OverlayFader>();
            fader.Play(cg, fIn, aMax, keep, fOut);
        }
    }

    /// <summary>
    /// 오버레이 자체에서 페이드 인/유지/페이드 아웃을 수행하고 마지막에 자기 자신을 파괴.
    /// (존이 네트워크로 파괴되어도 여기는 독립적으로 동작)
    /// </summary>
    public class OverlayFader : MonoBehaviour
    {
        public void Play(CanvasGroup cg, float fadeIn, float maxAlpha, float keep, float fadeOut)
        {
            StartCoroutine(Run(cg, fadeIn, maxAlpha, keep, fadeOut));
        }

        private IEnumerator Run(CanvasGroup cg, float fIn, float aMax, float keep, float fOut)
        {
            if (cg == null) yield break;
            cg.alpha = 0f;

            // Fade In
            if (fIn > 0f)
            {
                float t = 0f;
                while (t < fIn)
                {
                    t += Time.deltaTime;
                    cg.alpha = Mathf.Lerp(0f, aMax, t / fIn);
                    yield return null;
                }
            }
            else cg.alpha = aMax;

            // Keep
            if (keep > 0f) yield return new WaitForSeconds(keep);

            // Fade Out
            if (fOut > 0f)
            {
                float t = 0f;
                float start = cg.alpha;
                while (t < fOut)
                {
                    t += Time.deltaTime;
                    cg.alpha = Mathf.Lerp(start, 0f, t / fOut);
                    yield return null;
                }
            }
            else cg.alpha = 0f;

            Destroy(gameObject);
        }
    }
}
