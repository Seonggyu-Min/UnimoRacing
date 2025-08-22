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
                // �ٸ� �÷��̾� ������ PlayerItemInventory.RPC_ApplyItemLock ȣ��
                pv.RPC("RPCApplyItemLock", RpcTarget.Others, lockDuration);
            }

            Destroy(gameObject);
        }
    }
}
