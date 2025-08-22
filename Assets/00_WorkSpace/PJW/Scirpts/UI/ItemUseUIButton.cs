using System.Collections;
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
        }

        private void Start()
        {
            StartCoroutine(DelayedInit());
        }

        private IEnumerator DelayedInit()
        {
            yield return new WaitForSeconds(0.2f); // 플레이어 생성 대기

            var all = FindObjectsOfType<PlayerItemInventory>(true);
            foreach (var inv in all)
            {
                var pv = inv.GetComponent<PhotonView>() ?? inv.GetComponentInParent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    inventory = inv;
                    break;
                }
            }

            if (inventory == null)
            {
                Debug.LogError("[ItemUseUIButton] 로컬 플레이어의 인벤토리를 찾을 수 없습니다.");
                useButton.interactable = false;
                if (itemNameText != null)
                    itemNameText.text = "";
                yield break;
            }

            useButton.onClick.AddListener(inventory.UseItem);
            useButton.interactable = inventory.HasItem;

            // 아이템 보유 상태에 따라 버튼 상태/텍스트 갱신
            inventory.OnItemAvailabilityChanged += has =>
            {
                useButton.interactable = has;
                if (!has && itemNameText != null)
                    itemNameText.text = "";
            };

            inventory.OnItemAssigned += itemName =>
            {
                if (itemNameText != null)
                    itemNameText.text = $"{itemName}";
            };

            // 초기 표시
            itemNameText.text = inventory.HasItem ? $"{inventory.CurrentItemName()}" : "";
        }
    }
}
