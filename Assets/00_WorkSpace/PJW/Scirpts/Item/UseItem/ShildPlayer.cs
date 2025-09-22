using Photon.Pun;   
using System.Collections;
using UnityEngine;
using YTW;

namespace PJW
{
    [DisallowMultipleComponent]
    public class PlayerShield : MonoBehaviour
    {
        private bool isShieldActive;
        private Coroutine shieldRoutine;

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

            shieldRoutine = StartCoroutine(ShieldRoutine(duration));
        }

        private IEnumerator ShieldRoutine(float duration)
        {
            isShieldActive = true;
            yield return new WaitForSeconds(duration);
            isShieldActive = false;
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
    }
}
