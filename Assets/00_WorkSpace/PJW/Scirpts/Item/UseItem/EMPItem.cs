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

        [Header("Use Effect)")]
        [SerializeField] private string castResourceKey = "LightningNova";
        [SerializeField] private float castVfxHeight = 0.6f;
        [SerializeField] private bool castAlignToOwnerForward = true;

        [Header("Hit Effect")]
        [SerializeField] private string hitResourceKey = "Magic aura";
        [SerializeField] private float hitVfxHeight = 0.6f;
        [SerializeField] private bool alignToTargetForward = true;

        public void Use(GameObject owner)
        {
            if (!owner) { Destroy(gameObject); return; }

            var ownerPv = owner.GetComponentInParent<PhotonView>();
            if (!ownerPv) { Destroy(gameObject); return; }
            if (!ownerPv.IsMine) { Destroy(gameObject); return; }

            // 1) 캐스트 VFX 생성 + 오너에게 바로 부착
            var cast = TrySpawnVfxNetwork(
                castResourceKey,
                owner.transform.position + Vector3.up * castVfxHeight,
                castAlignToOwnerForward ? owner.transform.forward : Vector3.forward
            );
            AttachIfPossible(cast, ownerPv.ViewID, castVfxHeight, castAlignToOwnerForward);

            // 2) 타겟 탐색
            var target = FindTarget(owner);
            if (!target) { Destroy(gameObject); return; }
            var targetPv = target.GetComponentInParent<PhotonView>();
            if (!targetPv) { Destroy(gameObject); return; }

            // 3) 효과 적용 (타겟 소유자 전용)
            targetPv.RPC("RpcApplyRankStop", targetPv.Owner, stopDuration);

            // 4) 히트 VFX 생성 + 타겟에 바로 부착
            var hit = TrySpawnVfxNetwork(
                hitResourceKey,
                targetPv.transform.position + Vector3.up * hitVfxHeight,
                alignToTargetForward ? targetPv.transform.forward : Vector3.forward
            );
            AttachIfPossible(hit, targetPv.ViewID, hitVfxHeight, alignToTargetForward);

            Destroy(gameObject);
        }

        // 생성 후 GameObject 반환
        private GameObject TrySpawnVfxNetwork(string resourceKey, Vector3 pos, Vector3 forward)
        {
            if (string.IsNullOrEmpty(resourceKey)) return null;

            var prefab = Resources.Load<GameObject>(resourceKey);
            if (!prefab) return null;

            try
            {
                var rot = forward.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(forward, Vector3.up)
                    : Quaternion.identity;

                return PhotonNetwork.Instantiate(resourceKey, pos, rot, 0);
            }
            catch
            {
                return null;
            }
        }

        // 생성된 정확한 인스턴스의 PhotonView에 직접 RPC 전송 → 타겟 팔로우 지시
        private void AttachIfPossible(GameObject vfx, int targetViewId, float height, bool align)
        {
            if (!vfx) return;

            var vfxPv = vfx.GetComponent<PhotonView>();
            if (!vfxPv) return;

            var follower = vfx.GetComponent<VfxFollower>();
            if (!follower) return;

            vfxPv.RPC(nameof(VfxFollower.RpcSetupFollow), RpcTarget.All, targetViewId, height, align);
        }

        private GameObject FindTarget(GameObject owner)
        {
            var ownerPv = owner.GetComponentInParent<PhotonView>();
            var ownerCart = owner.GetComponentInParent<CinemachineDollyCart>();
            if (!ownerPv || !ownerCart || ownerCart.m_Path == null) return null;

            float myT = Mathf.Repeat(ownerCart.m_Position, 1f);
            bool looped = ownerCart.m_Path.Looped;

            var views = GameObject.FindObjectsOfType<PhotonView>();
            GameObject leader = null; float leaderT = -1f;
            GameObject nearestAhead = null; float bestAhead = float.PositiveInfinity;

            foreach (var v in views)
            {
                if (!v || !v.gameObject) continue;
                if (!string.IsNullOrEmpty(playerTag) && !v.gameObject.CompareTag(playerTag)) continue;
                if (v.ViewID == ownerPv.ViewID) continue;

                var cart = v.GetComponentInParent<CinemachineDollyCart>();
                if (!cart || cart.m_Path == null) continue;

                float t = Mathf.Repeat(cart.m_Position, 1f);
                if (t > leaderT) { leaderT = t; leader = v.gameObject; }

                float aheadDist = t - myT;
                if (looped) aheadDist = (aheadDist % 1f + 1f) % 1f;
                bool isAhead = looped ? (aheadDist > 0f && aheadDist < 1f) : (aheadDist > 0f);
                if (isAhead && aheadDist < bestAhead) { bestAhead = aheadDist; nearestAhead = v.gameObject; }
            }

            return (targetMode == TargetMode.Leader) ? leader : (nearestAhead != null ? nearestAhead : leader);
        }
    }
}
