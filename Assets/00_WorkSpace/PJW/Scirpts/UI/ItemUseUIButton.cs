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
        [Header("UI")]
        [SerializeField] private Button useButton;
        [SerializeField] private Image iconImage;


        private PlayerItemInventory inventory;

        private void Awake()
        {
            SetUI(null, false);
        }

        private void Start()
        {
            // 내 아이템 인벤토리만 가져오는 방어 코드
            var myInven = FindObjectsOfType<PlayerItemInventory>(true).FirstOrDefault(inven =>
                      {
                          var pv = inven.GetComponentInParent<PhotonView>();
                          return pv != null && pv.IsMine;
                      });

            if (myInven != null) Bind(myInven);

            if (useButton != null) useButton.onClick.AddListener(OnClickUse);
        }
        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.OnItemAssigned -= HandleAssigned;
                inventory.OnItemAvailabilityChanged -= HandleAvailability;
            }
            if (useButton != null) useButton.onClick.RemoveListener(OnClickUse);
        }

        private void Bind(PlayerItemInventory inv)
        {
            inventory = inv;
            inventory.OnItemAssigned += HandleAssigned;
            inventory.OnItemAvailabilityChanged += HandleAvailability;

            if (inventory.HasItem)
            {
                var icon = ItemSpriteRegistry.Instance.GetIcon(inventory.CurrentItemName());
                SetUI(icon, true);
            }
            else
            {
                SetUI(null, false);
            }
        }

        private void HandleAssigned(string prefabName)
        {
            var icon = ItemSpriteRegistry.Instance.GetIcon(prefabName);
            SetUI(icon, true);
        }

        private void HandleAvailability(bool hasItem)
        {
            if (!hasItem) SetUI(null, false);
        }

        private void OnClickUse()
        {
            if (inventory == null) return;
            inventory.UseItem();
        }

        private void SetUI(Sprite icon, bool hasItem)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;   
            }
            if (useButton != null)
            {
                useButton.gameObject.SetActive(true);        
                useButton.interactable = hasItem && icon != null;
            }
        }
    }
}
