using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

namespace PJW
{
    [RequireComponent(typeof(Button))]
    public class ItemUseUIButton : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private Button useButton;
        [SerializeField] private TextMeshProUGUI itemNameText;

        private PlayerItemInventory inventory;

        private void Awake()
        {
            if (useButton == null)
                useButton = GetComponent<Button>();

            if (itemNameText != null)
                itemNameText.gameObject.SetActive(false);
        }

        private void Start()
        {
            StartCoroutine(DelayedInit());
        }

        private IEnumerator DelayedInit()
        {
            yield return new WaitForSeconds(0.2f);

            // 내 소유 인벤토리만 바인딩
            inventory = FindObjectsOfType<PlayerItemInventory>(true)
                .FirstOrDefault(inv =>
                {
                    var pv = inv.GetComponent<PhotonView>() ?? inv.GetComponentInParent<PhotonView>();
                    return pv != null && pv.IsMine;
                });

            if (inventory == null)
            {
                SetInteractable(false);
                SetItemName("");
                yield break;
            }

            // 클릭 핸들러
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() =>
            {
                // 혹시라도 다른 인벤토리에 연결되었을 가능성 방지
                var pv = inventory.GetComponent<PhotonView>() ?? inventory.GetComponentInParent<PhotonView>();
                if (pv != null && !pv.IsMine) return;

                if (inventory.HasItem && inventory.CanUseItem)
                    inventory.UseItem();
            });

            // 이벤트 구독(보유 상태/이름 반영)
            inventory.OnItemAvailabilityChanged += OnAvailabilityChanged;
            inventory.OnItemAssigned += OnItemAssigned;

            // 초기 상태 반영
            OnAvailabilityChanged(inventory.HasItem);
            OnItemAssigned(inventory.CurrentItemName() ?? "");

            // 봉인(CanUseItem) 상태는 이벤트가 없으니 가볍게 모니터링
            StartCoroutine(MonitorLockState());
        }

        private IEnumerator MonitorLockState()
        {
            // 인벤토리가 사라질 때까지 주기적으로 버튼 상태 동기화
            while (inventory != null)
            {
                bool can = inventory.HasItem && inventory.CanUseItem;
                if (useButton != null && useButton.interactable != can)
                    SetInteractable(can);
                yield return null; 
            }
        }

        private void OnAvailabilityChanged(bool hasItem)
        {
            bool can = inventory != null && hasItem && inventory.CanUseItem;
            SetInteractable(can);

            if (!hasItem) SetItemName("");
        }

        private void OnItemAssigned(string itemName)
        {
            SetItemName(itemName);
            // 이름 갱신 시에도 버튼 활성 상태 재평가
            if (inventory != null)
                SetInteractable(inventory.HasItem && inventory.CanUseItem);
        }

        private void SetInteractable(bool value)
        {
            if (useButton != null)
                useButton.interactable = value;
        }

        private void SetItemName(string name)
        {
            if (itemNameText == null) return;

            bool show = !string.IsNullOrEmpty(name);
            itemNameText.text = show ? name : "";
            itemNameText.gameObject.SetActive(show);
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.OnItemAvailabilityChanged -= OnAvailabilityChanged;
                inventory.OnItemAssigned -= OnItemAssigned;
            }
        }
    }
}
