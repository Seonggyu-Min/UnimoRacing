using System.Collections;
using UnityEngine;
using Photon.Pun;
using YTW;

namespace PJW
{
    [DisallowMultipleComponent]
    public class PlayerShield : MonoBehaviour
    {
        private bool isShieldActive;
        private Coroutine shieldRoutine;

        public bool IsShieldActive => isShieldActive;

        [Header("사운드 키")]
        [SerializeField] private string sfxLoopKey = "Shield_SFX"; // AudioDB에서 Loop=true로 설정

        [Header("루프 정지 페이드(선택)")]
        [SerializeField] private float loopFadeOut = 0.15f;

        private AudioSource loopSrc;

        [Header("쉴드 VFX (지속 동안 표시)")]
        [SerializeField] private GameObject shieldVfxPrefab;                
        [SerializeField] private Vector3 vfxLocalOffset = new Vector3(0f, 0.6f, 0f);
        [SerializeField] private Vector3 vfxLocalEuler = Vector3.zero;
        [SerializeField] private Transform vfxAnchor;                       
        [SerializeField] private bool attachToOwner = true;                 
        [SerializeField] private float vfxCleanupDelay = 1.5f;              

        private GameObject vfxInstance;

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

            shieldRoutine = StartCoroutine(ShieldRoutine(duration));
        }

        private IEnumerator ShieldRoutine(float duration)
        {
            isShieldActive = true;

            // 오디오: 루프 SFX 시작
            loopSrc = AudioManager.Instance.PlaySFX(sfxLoopKey);

            // VFX: 쉴드 시작 시 스폰
            TrySpawnVfx();

            yield return new WaitForSeconds(duration);

            isShieldActive = false;

            // 오디오: 루프 SFX 종료
            if (loopSrc != null)
            {
                if (loopFadeOut > 0f)
                    AudioManager.Instance.StopSoundOn(loopSrc, loopFadeOut);
                else
                    AudioManager.Instance.StopLoopedSFX(loopSrc);

                loopSrc = null;
            }

            // VFX: 쉴드 종료 시 정리
            CleanupVfx();

            shieldRoutine = null;
        }

        /// <summary>
        /// 실드로 막았을 때 true. consume=true면 즉시 실드 종료(소모) 처리
        /// </summary>
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

                // 오디오: 루프 SFX 종료
                if (loopSrc != null)
                {
                    if (loopFadeOut > 0f)
                        AudioManager.Instance.StopSoundOn(loopSrc, loopFadeOut);
                    else
                        AudioManager.Instance.StopLoopedSFX(loopSrc);

                    loopSrc = null;
                }

                // VFX: 즉시 정리
                CleanupVfx();
            }

            return true;
        }

        private void TrySpawnVfx()
        {
            if (shieldVfxPrefab == null || vfxInstance != null)
                return;

            var anchor = vfxAnchor != null ? vfxAnchor : transform;

            Vector3 worldPos = anchor.TransformPoint(vfxLocalOffset);
            Quaternion worldRot = anchor.rotation * Quaternion.Euler(vfxLocalEuler);

            vfxInstance = Instantiate(shieldVfxPrefab, worldPos, worldRot, attachToOwner ? anchor : null);

            // 안전하게 Play 보장
            var psList = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < psList.Length; i++) psList[i].Play();
        }

        private void CleanupVfx()
        {
            if (vfxInstance == null) return;

            // 부드럽게 꺼지도록 StopEmitting
            var psList = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < psList.Length; i++)
                psList[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);

            Destroy(vfxInstance, Mathf.Max(0.01f, vfxCleanupDelay));
            vfxInstance = null;
        }
        // ------------------------------------------------
    }
}
