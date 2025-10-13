using Cinemachine;
using Photon.Pun;
using UnityEngine;
using YTW;

namespace PJW
{
    [DisallowMultipleComponent]
    public class LockItem : MonoBehaviour, IUsableItem
    {
        public enum TargetMode { Leader, NearestAhead }

        [Header("Target Rules")]
        [SerializeField] private TargetMode targetMode = TargetMode.NearestAhead;
        [SerializeField] private string playerTag = "Player";

        [Header("Effect Duration")]
        [Tooltip("히트 이펙트가 유지될 시간(초)")]
        [SerializeField] private float hitVfxDuration = 2f;

        [Tooltip("사용 이펙트가 유지될 시간(초)")]
        [SerializeField] private float castVfxDuration = 2f;

        [Header("Lock Effect")]
        [SerializeField] private float lockDuration = 3f;

        [Header("Use Effect")]
        [SerializeField] private string castResourceKey = "LightningNova";
        [SerializeField] private float castVfxHeight = 0.6f;
        [SerializeField] private bool castAlignToOwnerForward = true;

        [Header("Hit Effect")]
        [SerializeField] private string hitResourceKey = "Magic aura";
        [SerializeField] private float hitVfxHeight = 0.9f;
        [SerializeField] private bool alignToTargetForward = true;

        [Header("사운드 키")]
        [SerializeField] private string sfxUseKey = "Lock_Use";
        [SerializeField] private string sfxHitKey = "Lock_SFX";

        public void Use(GameObject owner)
        {
            if (!owner)
            {
                Destroy(gameObject);
                return;
            }

            var ownerPv = owner.GetComponentInParent<PhotonView>();
            if (!ownerPv || !ownerPv.IsMine)
            {
                Destroy(gameObject);
                return;
            }

            // 1) 캐스트 VFX 생성
            var cast = TrySpawnVfxNetwork(
                castResourceKey,
                owner.transform.position + Vector3.up * castVfxHeight,
                castAlignToOwnerForward ? owner.transform.forward : Vector3.forward
            );
            AttachIfPossible(cast, ownerPv.ViewID, castVfxHeight, castAlignToOwnerForward);
            AttachAutoDespawn(cast, castVfxDuration);

            // 2) 사용 사운드
            if (!string.IsNullOrEmpty(sfxUseKey))
                AudioManager.Instance.PlaySFX(sfxUseKey);

            // 3) 타겟 찾기
            var target = FindTarget(owner);
            if (!target)
            {
                Destroy(gameObject);
                return;
            }

            var targetPv = target.GetComponentInParent<PhotonView>();
            if (!targetPv)
            {
                Destroy(gameObject);
                return;
            }

            // 4) 피해자에게 잠금 적용
            targetPv.RPC("RPCApplyItemLock", targetPv.Owner, lockDuration);

            // 5) 히트 VFX 생성
            var hit = TrySpawnVfxNetwork(
                hitResourceKey,
                targetPv.transform.position + Vector3.up * hitVfxHeight,
                alignToTargetForward ? targetPv.transform.forward : Vector3.forward
            );
            AttachIfPossible(hit, targetPv.ViewID, hitVfxHeight, alignToTargetForward);
            AttachAutoDespawn(hit, hitVfxDuration);

            // 6) 피격 사운드
            if (!string.IsNullOrEmpty(sfxHitKey))
                AudioManager.Instance.PlaySFX(sfxHitKey);

            Destroy(gameObject);
        }

        // ====== 유틸 ======

        // 네트워크에 VFX 프리팹 생성 후 GameObject 반환
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

        // 생성된 VFX의 PhotonView에 RPC로 팔로우 세팅
        private void AttachIfPossible(GameObject vfx, int targetViewId, float height, bool align)
        {
            if (!vfx) return;

            var vfxPv = vfx.GetComponent<PhotonView>();
            if (!vfxPv) return;

            var follower = vfx.GetComponent<VfxFollower>();
            if (!follower) return;

            vfxPv.RPC(nameof(VfxFollower.RpcSetupFollow), RpcTarget.All, targetViewId, height, align);
        }

        // 자동 삭제 기능 연결
        private void AttachAutoDespawn(GameObject vfx, float duration)
        {
            if (!vfx || duration <= 0f) return;

            var pv = vfx.GetComponent<PhotonView>();
            if (pv == null)
            {
                Destroy(vfx, duration);
                return;
            }

            var ad = vfx.GetComponent<AutoDespawn>();
            if (ad == null) ad = vfx.AddComponent<AutoDespawn>();
            ad.lifeSeconds = duration;
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

            return (targetMode == TargetMode.Leader)
                ? leader
                : (nearestAhead != null ? nearestAhead : leader);
        }
    }
}
