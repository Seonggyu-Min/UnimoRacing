using System;
using System.Collections;
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
        public event Action<string> OnItemAssigned; // 아이템 이름 전달 이벤트

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

            // 텍스트 UI에 아이템 이름 알리기
            OnItemAssigned?.Invoke(currentItemPrefab.name);

            OnItemAvailabilityChanged?.Invoke(HasItem);
        }

        public void UseItem()
        {
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
            ApplyItemLock(duration);
        }

        private IEnumerator LockRoutine(float duration)
        {
            CanUseItem = false;
            yield return new WaitForSeconds(duration);
            CanUseItem = true;
            lockRoutine = null;
        }
    }
}
