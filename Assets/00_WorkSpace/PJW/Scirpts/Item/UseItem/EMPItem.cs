using UnityEngine;
using Photon.Pun;
using Cinemachine;

namespace PJW
{
    [DisallowMultipleComponent]
    public class EMPItem : MonoBehaviour, IUsableItem
    {
        public enum TargetMode { Leader, NearestAhead }

        [Header("Target Rules")]
        [SerializeField] private TargetMode targetMode = TargetMode.NearestAhead;
        [SerializeField] private string playerTag = "Player";

        [Header("Effect")]
        [SerializeField] private float stopDuration = 1.5f;

        public void Use(GameObject owner)
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            var ownerPv = owner.GetComponentInParent<PhotonView>();
            if (ownerPv == null || !ownerPv.IsMine)
            {
                Destroy(gameObject);
                return;
            }

            var target = FindTarget(owner);
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            var targetView = target.GetComponentInParent<PhotonView>();
            if (targetView == null)
            {
                Destroy(gameObject);
                return;
            }

            // 대상 소유자에게만 정지 효과 적용
            targetView.RPC("RpcApplyRankStop", targetView.Owner, stopDuration);

            Destroy(gameObject);
        }

        private GameObject FindTarget(GameObject owner)
        {
            var ownerPv = owner.GetComponentInParent<PhotonView>();
            var ownerCart = owner.GetComponentInParent<CinemachineDollyCart>();

            if (ownerPv == null || ownerCart == null || ownerCart.m_Path == null)
                return null;

            float myT = Mathf.Repeat(ownerCart.m_Position, 1f);
            bool looped = ownerCart.m_Path.Looped;

            var views = GameObject.FindObjectsOfType<PhotonView>();
            GameObject leader = null;
            float leaderT = -1f;

            GameObject nearestAhead = null;
            float bestAhead = float.PositiveInfinity;

            foreach (var v in views)
            {
                if (v == null || v.gameObject == null) continue;
                if (!string.IsNullOrEmpty(playerTag) && !v.gameObject.CompareTag(playerTag)) continue;

                // 자기 자신 제외
                if (ownerPv != null && v.ViewID == ownerPv.ViewID) continue;

                var cart = v.GetComponentInParent<CinemachineDollyCart>();
                if (cart == null || cart.m_Path == null) continue;

                float t = Mathf.Repeat(cart.m_Position, 1f);

                // Leader (진행도 최댓값)
                if (t > leaderT)
                {
                    leaderT = t;
                    leader = v.gameObject;
                }

                // 앞사람 (루프 고려)
                float aheadDist = t - myT;
                if (looped) aheadDist = (aheadDist % 1f + 1f) % 1f;

                bool isAhead = looped ? (aheadDist > 0f && aheadDist < 1f) : (aheadDist > 0f);
                if (isAhead && aheadDist < bestAhead)
                {
                    bestAhead = aheadDist;
                    nearestAhead = v.gameObject;
                }
            }

            switch (targetMode)
            {
                case TargetMode.Leader:
                    return leader;
                case TargetMode.NearestAhead:
                    return nearestAhead != null ? nearestAhead : leader; // 폴백으로 리더
                default:
                    return null;
            }
        }
    }
}
