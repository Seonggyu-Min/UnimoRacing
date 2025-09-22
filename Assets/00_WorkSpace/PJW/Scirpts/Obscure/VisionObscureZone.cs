using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
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

        private bool triggered;
        private Collider zoneCollider;
        private Renderer[] renderers;
        private ParticleSystem[] particles;

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;
            renderers = GetComponentsInChildren<Renderer>(true);
            particles = GetComponentsInChildren<ParticleSystem>(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (oneShot && triggered) return;

            var targetPv = other.GetComponentInParent<PhotonView>();
            if (targetPv == null) return;

            // 동일한 플레이어로 중복 트리거 방지(선택)
            triggered = true;

            // 존 비활성 비주얼 처리(콜라이더 닫고 메쉬 숨김)
            DisableZoneVisuals();

            // 해당 플레이어의 "오너 클라이언트"에서만 화면 오버레이 띄우기
            photonView.RPC(nameof(RpcApplyObscureOnLocal), targetPv.Owner,
                obscureOverlayResource, duration, fadeIn, maxAlpha, fadeOut);

            AudioManager.Instance.PlaySFX(sfxHitKey);

            // 원샷이면 잠시 뒤 파괴, 아니면 재사용도 가능
            if (oneShot)
            {
                // 효과 전달 후 짧은 시간 뒤 네트워크 오브젝트 제거
                StartCoroutine(CoDestroyAfter(0.5f));
            }
        }

        private void DisableZoneVisuals()
        {
            if (zoneCollider != null) zoneCollider.enabled = false;
            if (renderers != null)
            {
                foreach (var r in renderers) r.enabled = false;
            }
            if (particles != null)
            {
                foreach (var ps in particles) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private IEnumerator CoDestroyAfter(float t)
        {
            yield return new WaitForSeconds(t);
            if (photonView != null && photonView.IsMine)
                PhotonNetwork.Destroy(gameObject);
        }

        /// <summary>
        /// 대상 플레이어의 로컬 클라이언트에서만 실행되는 RPC.
        /// Resources에서 오버레이 프리팹을 로드해 Canvas 아래에 붙이고
        /// 페이드 인/유지/아웃 후 제거.
        /// </summary>
        [PunRPC]
        private void RpcApplyObscureOnLocal(string resourceName, float keep, float fIn, float aMax, float fOut)
        {
            var prefab = Resources.Load<GameObject>(resourceName);
            if (prefab == null)
            {
                Debug.LogError($"[VisionObscureZone] Resources '{resourceName}' not found.");
                return;
            }

            // 최상단 Canvas 탐색(없으면 간이 Canvas 생성)
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

            // CanvasGroup이 있으면 알파로 페이딩, 없으면 추가
            var cg = overlay.GetComponent<CanvasGroup>();
            if (cg == null) cg = overlay.AddComponent<CanvasGroup>();

            // 안전장치: 이미지가 있으면 RaycastTarget 꺼서 입력 방해 방지(원하면 켜세요)
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
