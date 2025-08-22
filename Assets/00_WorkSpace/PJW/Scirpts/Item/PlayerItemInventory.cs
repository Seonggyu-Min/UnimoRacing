using System;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public interface IUsableItem { void Use(GameObject owner); }

    public class PlayerItemInventory : MonoBehaviour
    {
        [Header("Preview")]
        [SerializeField] private Transform attachPoint;
        [Header("Owner")]
        [SerializeField] private PhotonView ownerView; 

        private GameObject currentItemPrefab;
        public bool HasItem => currentItemPrefab != null;
        public event Action<bool> OnItemAvailabilityChanged;
        public event Action<string> OnItemAssigned; // 아이템 이름 전달 이벤트

        private void Awake()
        {
            if (ownerView == null)
                ownerView = GetComponent<PhotonView>() ?? GetComponentInParent<PhotonView>();

            if (ownerView == null);
        }

        public void AssignItemPrefab(GameObject itemPrefab)
        {
            if (itemPrefab == null)
            {
                return;
            }

            if (HasItem) ClearItem();

            currentItemPrefab = itemPrefab;
            if (attachPoint != null)
            {
                var preview = Instantiate(currentItemPrefab, attachPoint);
                preview.name = $"{itemPrefab.name}_Preview";
                preview.SetActive(false);
            }

            // 텍스트 UI에 아이템 이름 알리기
            OnItemAssigned?.Invoke(currentItemPrefab.name);

            OnItemAvailabilityChanged?.Invoke(HasItem);
        }

        public void UseItem()
        {
            if (ownerView != null && !ownerView.IsMine)
            {
                return;
            }

            if (!HasItem)
            {
                return;
            }

            if (currentItemPrefab == null)
            {
                return;
            }

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
            if (attachPoint != null)
            {
                for (int i = attachPoint.childCount - 1; i >= 0; i--)
                    Destroy(attachPoint.GetChild(i).gameObject);
            }
        }

        private string OwnerTag()
        {
            if (ownerView == null) return $"{name}/NoPV";
            return $"actor={ownerView.OwnerActorNr}, IsMine={ownerView.IsMine}, obj={ownerView.name}";
        }
        public string CurrentItemName()
        {
            return currentItemPrefab != null ? currentItemPrefab.name : null;
        }
    }
}
