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
        [Header("UI ����")]
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
            yield return new WaitForSeconds(0.2f); // �÷��̾� ���� ���

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
                Debug.LogError("[ItemUseUIButton] ���� �÷��̾��� �κ��丮�� ã�� �� �����ϴ�.");
                useButton.interactable = false;
                if (itemNameText != null)
                    itemNameText.text = "";
                yield break;
            }

            useButton.onClick.AddListener(inventory.UseItem);
            useButton.interactable = inventory.HasItem;

            // ������ ���� ���¿� ���� ��ư ����/�ؽ�Ʈ ����
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

            // �ʱ� ǥ��
            itemNameText.text = inventory.HasItem ? $"{inventory.CurrentItemName()}" : "";
        }
    }
}
