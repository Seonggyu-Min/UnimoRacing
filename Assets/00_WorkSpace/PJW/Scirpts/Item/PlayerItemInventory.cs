using System;
using System.Collections;
using System.Linq;               
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public interface IUsableItem { void Use(GameObject owner); }

    public class PlayerItemInventory : MonoBehaviour
    {
        [Header("Owner")]
        [SerializeField] private PhotonView ownerView;

        private GameObject currentItemPrefab;
        public bool HasItem => currentItemPrefab != null;
        public bool CanUseItem { get; private set; } = true;
        public event Action<bool> OnItemAvailabilityChanged;
        public event Action<string> OnItemAssigned;

        private Coroutine lockRoutine;

        private void Awake()
        {
            if (ownerView == null)
                ownerView = GetComponent<PhotonView>() ?? GetComponentInParent<PhotonView>();
        }

        public void AssignItemPrefab(GameObject itemPrefab)
        {
            if (itemPrefab == null) return;

            if (HasItem) ClearItem();

            currentItemPrefab = itemPrefab;

            OnItemAssigned?.Invoke(currentItemPrefab.name);

            OnItemAvailabilityChanged?.Invoke(HasItem);
        }

        public void UseItem()
        {
            if (!CanUseItem) return;                        

            if (ownerView != null && !ownerView.IsMine) return;
            if (!HasItem || currentItemPrefab == null) return;

            var go = Instantiate(currentItemPrefab, transform.position, Quaternion.identity);
            go.name = $"{currentItemPrefab.name}_Inst";
            var usable = go.GetComponent<IUsableItem>();

            if (usable == null)
            {
                Destroy(go);
                return;
            }

            usable.Use(ownerView != null ? ownerView.gameObject : gameObject);
            ClearItem();
            OnItemAvailabilityChanged?.Invoke(HasItem);
        }

        private void ClearItem()
        {
            if (currentItemPrefab != null)
                currentItemPrefab = null;
        }

        public string CurrentItemName()
        {
            return currentItemPrefab != null ? currentItemPrefab.name : null;
        }

        public void ApplyItemLock(float duration)
        {
            if (lockRoutine != null) StopCoroutine(lockRoutine);
            lockRoutine = StartCoroutine(LockRoutine(duration));
        }

        [PunRPC]
        private void RPCApplyItemLock(float duration)
        {
            var myInv = FindObjectsOfType<PlayerItemInventory>(true)
                .FirstOrDefault(inv =>
                {
                    var v = inv.ownerView ?? inv.GetComponent<PhotonView>() ?? inv.GetComponentInParent<PhotonView>();
                    return v != null && v.IsMine;
                });

            if (myInv != null)
            {
                myInv.ApplyItemLock(duration);
            }
            else
            {
                ApplyItemLock(duration);
            }
        }

        private IEnumerator LockRoutine(float duration)
        {
            CanUseItem = false;
            yield return new WaitForSeconds(duration);
            CanUseItem = true;
            lockRoutine = null;
        }

        [PunRPC]
        private void RpcObscureOpponents(int ownerActorNr, float duration, float fadeIn, float maxAlpha, float fadeOut)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == ownerActorNr) return;  
            VisionObscureController.EnsureInScene().Obscure(duration, fadeIn, maxAlpha, fadeOut);
        }
    }
}
