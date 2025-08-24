using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class LockItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private float lockDuration = 5f;

        public void Use(GameObject owner)
        {
            var ownerView = owner.GetComponent<PhotonView>() ?? owner.GetComponentInParent<PhotonView>();
            if (ownerView != null && ownerView.IsMine)
            {
                ownerView.RPC("RPCApplyItemLock", RpcTarget.Others, lockDuration);
            }

            Destroy(gameObject);
        }
    }
}
