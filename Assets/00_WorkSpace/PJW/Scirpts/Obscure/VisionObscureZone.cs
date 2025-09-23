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
        [SerializeField] private GameObject particleObject;     // 파티클/비주얼 이펙트를 담은 게임오브젝트
        [Tooltip("true면 particleObject를 프리팹으로 보고 현재 위치에 인스턴스 생성")]
        [SerializeField] private bool instantiateParticle = false;

        private bool triggered;
        private Collider zoneCollider;
        private Renderer[] renderers;
        private GameObject particleRuntime; // 인스턴스된 오브젝트 보관(instantiateParticle=true 일 때)

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
                    // 프리팹으로 간주하고 현재 위치/회전으로 생성
                    particleRuntime = Instantiate(particleObject, transform.position, transform.rotation, transform);
                    ShowEffectObject(particleRuntime, true);
                }
                else
                {
                    // 이미 씬에 있는(자식 등) 오브젝트를 활성화
                    ShowEffectObject(particleObject, true);
                }
            }

            // lifetime이 지정된 경우 자동 파괴
            if (lifetime > 0f)
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
            AudioManager.Instance.PlaySFX(sfxHitKey);

            // 원샷 모드면 충돌 후 잠시 뒤 파괴
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

            // 이펙트 정지/비활성
            var target = particleRuntime != null ? particleRuntime : particleObject;
            if (target != null)
                ShowEffectObject(target, false);
        }

        /// <summary>
        /// 이펙트 오브젝트를 보이거나 숨김. 내부의 ParticleSystem/VisualEffect가 있으면 Play/Stop까지 수행.
        /// </summary>
        private void ShowEffectObject(GameObject obj, bool show)
        {
            if (obj == null) return;

            // 우선 활성/비활성
            obj.SetActive(true); // Play/Stop을 위해 일단 켰다가 처리
            // ParticleSystem 제어
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

            // Visual Effect Graph 제어(있다면)
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

            // 최종 활성 상태
            if (!show)
                obj.SetActive(false);
        }

        private IEnumerator CoDestroyAfter(float t)
        {
            yield return new WaitForSeconds(t);

            if (photonView != null && photonView.IsMine)
                PhotonNetwork.Destroy(gameObject);
            else if (photonView == null)
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

            var img = overlay.GetComponentInChildren<Image>(true);
            if (img != null) img.raycastTarget = false;

            overlay.SetActive(true);
            StartCoroutine(CoFadeOverlayAndDestroy(overlay, cg, fIn, aMax, keep, fOut));
        }

        private IEnumerator CoFadeOverlayAndDestroy(GameObject obj, CanvasGroup cg, float fIn, float aMax, float keep, float fOut)
        {
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

            if (obj != null) Destroy(obj);
        }
    }
}
