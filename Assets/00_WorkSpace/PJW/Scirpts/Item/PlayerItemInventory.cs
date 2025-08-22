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

        private void Awake()
        {
            if (ownerView == null)
                ownerView = GetComponent<PhotonView>() ?? GetComponentInParent<PhotonView>();

            if (ownerView == null)
                Debug.LogWarning($"[Inventory] PhotonView 없음 (obj={name}) — 싱글이라면 무시해도 됨");
        }

        public void AssignItemPrefab(GameObject itemPrefab)
        {
            if (itemPrefab == null)
            {
                Debug.LogWarning($"[Inventory] AssignItemPrefab(null) (obj={name})");
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

            Debug.Log($"[Inventory] Assigned '{itemPrefab.name}' to {OwnerTag()}  HasItem={HasItem}");
            OnItemAvailabilityChanged?.Invoke(HasItem);
        }

        public void UseItem()
        {
            // 소유자 체크(멀티에서 가장 흔한 원인)
            if (ownerView != null && !ownerView.IsMine)
            {
                Debug.LogWarning($"[Inventory] UseItem 호출 무시 — 소유자가 아님. UI가 다른 플레이어를 가리키는지 확인. ({OwnerTag()})");
                return;
            }

            if (!HasItem)
            {
                Debug.LogWarning($"[Inventory] UseItem 실패 — 보유 아이템 없음. ({OwnerTag()})");
                return;
            }

            if (currentItemPrefab == null)
            {
                Debug.LogWarning($"[Inventory] UseItem 실패 — currentItemPrefab null ({OwnerTag()})");
                return;
            }

            var go = Instantiate(currentItemPrefab, transform.position, Quaternion.identity);
            go.name = $"{currentItemPrefab.name}_Inst";
            var usable = go.GetComponent<IUsableItem>();

            if (usable == null)
            {
                Debug.LogWarning($"[Inventory] '{currentItemPrefab.name}' 에 IUsableItem 없음  사용 불가. ({OwnerTag()})");
                Destroy(go);
                return;
            }

            Debug.Log($"[Inventory] Use '{currentItemPrefab.name}' 시작 ({OwnerTag()})");
            usable.Use(ownerView != null ? ownerView.gameObject : gameObject);
            ClearItem();
            OnItemAvailabilityChanged?.Invoke(HasItem);
            Debug.Log($"[Inventory] Use 완료  인벤토리 비움 ({OwnerTag()})");
        }

        private void ClearItem()
        {
            if (currentItemPrefab != null)
                Debug.Log($"[Inventory] ClearItem '{currentItemPrefab.name}' ({OwnerTag()})");

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
    }
}
