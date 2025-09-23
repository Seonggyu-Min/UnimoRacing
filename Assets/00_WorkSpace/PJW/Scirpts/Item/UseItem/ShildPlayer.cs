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

            loopSrc = AudioManager.Instance.PlaySFX(sfxLoopKey);

            yield return new WaitForSeconds(duration);

            isShieldActive = false;

            if (loopSrc != null)
            {
                if (loopFadeOut > 0f)
                    AudioManager.Instance.StopSoundOn(loopSrc, loopFadeOut);
                else
                    AudioManager.Instance.StopLoopedSFX(loopSrc);

                loopSrc = null;
            }

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

                if (loopSrc != null)
                {
                    if (loopFadeOut > 0f)
                        AudioManager.Instance.StopSoundOn(loopSrc, loopFadeOut);
                    else
                        AudioManager.Instance.StopLoopedSFX(loopSrc);

                    loopSrc = null;
                }
            }

            return true;
        }
    }
}
