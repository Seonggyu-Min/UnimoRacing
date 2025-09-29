using System.Collections;
using UnityEngine;
using Photon.Pun;
using Cinemachine;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhotonView))]
    public class RankStopApplier : MonoBehaviourPun
    {
        private Coroutine running;
        private float cachedKartSpeed = -1f;
        private float cachedCmSpeed = -1f;

        [PunRPC]
        private void RpcApplyRankStop(float duration, PhotonMessageInfo info)
        {
            if (!photonView.IsMine)
            {
                // 소유자만 실제 속도 제어
                return;
            }

            if (running != null) StopCoroutine(running);
            running = StartCoroutine(ApplyStopRoutine(duration));
        }

        private IEnumerator ApplyStopRoutine(float duration)
        {
            var prd = GetComponentInParent<PlayerRaceData>();
            var cart = GetComponentInParent<CinemachineDollyCart>();

            // 1) 현재 속도 캐싱
            cachedKartSpeed = prd ? prd.KartSpeed : -1f;
            cachedCmSpeed = cart ? cart.m_Speed : -1f;

            // 2) 정지
            if (prd != null)
            {
                prd.SetKartSpeed(0f);  
            }
            if (cart != null)
            {
                cart.m_Speed = 0f;
            }

            yield return new WaitForSeconds(duration);

            // 3) 복구
            if (prd != null && cachedKartSpeed >= 0f)
            {
                prd.SetKartSpeed(cachedKartSpeed);
            }
            if (cart != null && cachedCmSpeed >= 0f)
            {
                cart.m_Speed = cachedCmSpeed;
            }

            running = null;
        }
    }
}
