using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

namespace PJW
{
    /// <summary>
    /// ���� �÷��̾��� �κ��丮�� ã�� ������ ��� ��ư�� �ڵ����� ����
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ItemUseUIButton : MonoBehaviour
    {
        [Tooltip("��� ��ư (������ �ڵ����� �ڱ� Button ������Ʈ ���)")]
        [SerializeField] private Button useButton;

        private PlayerItemInventory inventory;

        private void Awake()
        {
            // ��ư�� �������� �ʾҴٸ� �ڱ� �ڽſ��� ã����
            if (useButton == null)
                useButton = GetComponent<Button>();
        }

        private void Start()
        {
            // �÷��̾� ������ ��ٸ��� ���� ������ �� �κ��丮 ã��
            StartCoroutine(DelayedInit());
        }

        private IEnumerator DelayedInit()
        {
            yield return new WaitForSeconds(0.2f); // �÷��̾� �ν��Ͻ�ȭ �ð� Ȯ��

            // ���� ��� �κ��丮 �� IsMine�� �͸� ����
            var allInventories = FindObjectsOfType<PlayerItemInventory>(true);
            foreach (var inv in allInventories)
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
                yield break;
            }

            // ��ư Ŭ�� �� ������ ��� ����
            useButton.onClick.AddListener(inventory.UseItem);

            // �ʱ� ����
            useButton.interactable = inventory.HasItem;

            // �κ��丮 ���°� �ٲ� ������ ��ư ���ͷ��� ����
            inventory.OnItemAvailabilityChanged += hasItem =>
            {
                useButton.interactable = hasItem;
            };

            Debug.Log($"[ItemUseUIButton] ����� �κ��丮: {inventory.name}");
        }
    }
}
