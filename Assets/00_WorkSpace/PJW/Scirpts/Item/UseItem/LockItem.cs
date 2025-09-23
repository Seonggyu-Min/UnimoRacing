using Photon.Pun;
using UnityEngine;
using YTW;

namespace PJW
{
    public class LockItem : MonoBehaviour, IUsableItem
    {
        [Header("Lock Settings")]
        [SerializeField] private float lockDuration = 5f;

        [Header("사운드 키")]
        [SerializeField] private string sfxUseKey = "Lock_Use";

        [Header("파티클 프리팹 (플레이어 따라감)")]
        [SerializeField] private GameObject followVfxPrefab;

        [Header("파티클 위치 오프셋 (플레이어 기준)")]
        [SerializeField] private Vector3 followVfxOffset = Vector3.zero;

        public void Use(GameObject owner)
        {
            var ownerView = owner.GetComponent<PhotonView>() ?? owner.GetComponentInParent<PhotonView>();
            if (ownerView != null && ownerView.IsMine)
            {
                // 1. 사운드 재생
                if (!string.IsNullOrEmpty(sfxUseKey))
                    AudioManager.Instance.PlaySFX(sfxUseKey);

                // 2. 파티클 생성 → 플레이어에 붙이기
                if (followVfxPrefab != null)
                {
                    var vfx = Instantiate(followVfxPrefab, owner.transform);

                    // Inspector에서 조절 가능한 오프셋 적용
                    vfx.transform.localPosition = followVfxOffset;
                    vfx.transform.localRotation = Quaternion.identity;

                    // 파티클 재생
                    var ps = vfx.GetComponentsInChildren<ParticleSystem>(true);
                    for (int i = 0; i < ps.Length; i++) ps[i].Play();

                    // lockDuration 후 자동 제거
                    Destroy(vfx, lockDuration);
                }

                // 3. RPC로 락 효과 적용
                ownerView.RPC("RPCApplyItemLock", RpcTarget.Others, lockDuration);
            }

            // 4. LockItem 자기 자신 제거
            Destroy(gameObject);
        }
    }
}
