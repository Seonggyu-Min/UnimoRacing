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
        [Header("UI ����")]
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

            // �� ���� �κ��丮�� ���ε�
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

            // Ŭ�� �ڵ鷯
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() =>
            {
                // Ȥ�ö� �ٸ� �κ��丮�� ����Ǿ��� ���ɼ� ����
                var pv = inventory.GetComponent<PhotonView>() ?? inventory.GetComponentInParent<PhotonView>();
                if (pv != null && !pv.IsMine) return;

                if (inventory.HasItem && inventory.CanUseItem)
                    inventory.UseItem();
            });

            // �̺�Ʈ ����(���� ����/�̸� �ݿ�)
            inventory.OnItemAvailabilityChanged += OnAvailabilityChanged;
            inventory.OnItemAssigned += OnItemAssigned;

            // �ʱ� ���� �ݿ�
            OnAvailabilityChanged(inventory.HasItem);
            OnItemAssigned(inventory.CurrentItemName() ?? "");

            // ����(CanUseItem) ���´� �̺�Ʈ�� ������ ������ ����͸�
            StartCoroutine(MonitorLockState());
        }

        private IEnumerator MonitorLockState()
        {
            // �κ��丮�� ����� ������ �ֱ������� ��ư ���� ����ȭ
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
            // �̸� ���� �ÿ��� ��ư Ȱ�� ���� ����
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
