using System.Collections;
using UnityEngine;
using Photon.Pun;   

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
    }
}
