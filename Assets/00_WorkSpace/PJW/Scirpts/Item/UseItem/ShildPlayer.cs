using Photon.Pun;
using System.Collections;
using UnityEngine;
using YTW;

namespace PJW
{
    [DisallowMultipleComponent]
    public class PlayerShield : MonoBehaviour
    {
        [Header("Shield Visual Effect")]
        [SerializeField] private GameObject shieldEffectPrefab;    // 실드 유지동안 보여줄 이펙트
        [SerializeField] private Vector3 effectLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 effectLocalEuler = Vector3.zero;
        [SerializeField] private bool destroyEffectOnDisable = false; // false면 재사용(비활성/활성 전환)

        private bool isShieldActive;
        private Coroutine shieldRoutine;

        // 런타임 이펙트 인스턴스/파티클 캐시
        private GameObject shieldEffectInstance;
        private ParticleSystem[] cachedParticles;

        public bool IsShieldActive => isShieldActive;

        [PunRPC]
        public void RpcActivateShield(float duration)
        {
            ActivateShield(duration);
        }

        [PunRPC]
        public void RpcConsumeShield()
        {
            SuccessShield(consume: true);
        }

        public void ActivateShield(float duration)
        {
            if (shieldRoutine != null)
                StopCoroutine(shieldRoutine);

            // 이펙트 켜기
            EnableShieldEffect();

            shieldRoutine = StartCoroutine(ShieldRoutine(duration));
        }

        private IEnumerator ShieldRoutine(float duration)
        {
            isShieldActive = true;
            yield return new WaitForSeconds(duration);
            isShieldActive = false;

            // 이펙트 끄기
            DisableShieldEffect();

            shieldRoutine = null;
        }

        public bool SuccessShield(bool consume = false)
        {
            if (!isShieldActive) return false;

            if (consume)
            {
                if (shieldRoutine != null)
                {
                    StopCoroutine(shieldRoutine);
                    shieldRoutine = null;
                }
                isShieldActive = false;

                // 소비 시에도 이펙트 끄기
                DisableShieldEffect();
            }
            return true;
        }

        [PunRPC]
        public void RpcPlayShieldLoop(string loopKey, float duration)
        {
            PlayShieldLoop(loopKey, duration);
        }

        public void PlayShieldLoop(string loopKey, float duration)
        {
            if (string.IsNullOrEmpty(loopKey)) return;

            var src = AudioManager.Instance.PlaySFX(loopKey);
            if (src != null && src.loop)
            {
                StartCoroutine(StopLoopAfter(src, duration));
            }
        }

        private IEnumerator StopLoopAfter(AudioSource src, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (src != null)
            {
                AudioManager.Instance.StopLoopedSFX(src);
            }
        }

        // ---------- Effect Helpers ----------
        private void EnableShieldEffect()
        {
            if (shieldEffectPrefab == null)
                return;

            // 이미 생성돼 있으면 재사용
            if (shieldEffectInstance == null)
            {
                shieldEffectInstance = Instantiate(shieldEffectPrefab, transform);
                shieldEffectInstance.transform.localPosition = effectLocalPosition;
                shieldEffectInstance.transform.localRotation = Quaternion.Euler(effectLocalEuler);
                cachedParticles = shieldEffectInstance.GetComponentsInChildren<ParticleSystem>(true);
            }

            if (!shieldEffectInstance.activeSelf)
                shieldEffectInstance.SetActive(true);

            // 파티클들 재생
            if (cachedParticles != null)
            {
                for (int i = 0; i < cachedParticles.Length; i++)
                {
                    if (cachedParticles[i] == null) continue;
                    cachedParticles[i].Play(true);
                }
            }
        }

        private void DisableShieldEffect()
        {
            if (shieldEffectInstance == null)
                return;

            // 파티클들 정지
            if (cachedParticles != null)
            {
                for (int i = 0; i < cachedParticles.Length; i++)
                {
                    if (cachedParticles[i] == null) continue;
                    cachedParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            if (destroyEffectOnDisable)
            {
                Destroy(shieldEffectInstance);
                shieldEffectInstance = null;
                cachedParticles = null;
            }
            else
            {
                // 다음에 다시 사용할 수 있도록 비활성화
                if (shieldEffectInstance.activeSelf)
                    shieldEffectInstance.SetActive(false);
            }
        }

        // 안전장치: 컴포넌트가 비활성/파괴될 때 이펙트 정리
        private void OnDisable()
        {
            if (isShieldActive)
            {
                isShieldActive = false;
            }
            DisableShieldEffect();
        }

        private void OnDestroy()
        {
            DisableShieldEffect();
        }
    }
}
