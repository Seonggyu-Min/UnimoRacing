using Photon.Pun;
using UnityEngine;
using YTW;

namespace PJW
{
    public class LockItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private float lockDuration = 5f;
        [SerializeField] private string sfxUseKey = "Lock_Use";

        public void Use(GameObject owner)
        {
            var ownerView = owner.GetComponent<PhotonView>() ?? owner.GetComponentInParent<PhotonView>();
            if (ownerView != null && ownerView.IsMine)
            {
                AudioManager.Instance.PlaySFX(sfxUseKey);

                ownerView.RPC("RPCApplyItemLock", RpcTarget.Others, lockDuration);
            }

            Destroy(gameObject);
        }
    }
}
