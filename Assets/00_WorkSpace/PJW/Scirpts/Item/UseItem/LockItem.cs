using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class LockItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private float lockDuration = 5f;

        public void Use(GameObject owner)
        {
            var pv = owner.GetComponent<PhotonView>() ?? owner.GetComponentInParent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                // 다른 플레이어 전부의 PlayerItemInventory.RPC_ApplyItemLock 호출
                pv.RPC("RPCApplyItemLock", RpcTarget.Others, lockDuration);
            }

            Destroy(gameObject);
        }
    }
}
