using System;
using System.Collections;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    [DisallowMultipleComponent]
    public class RandomItemBox : MonoBehaviourPun
    {
        [Serializable]
        private class WeightedItem
        {
            public GameObject itemPrefab;
            public int weight = 1; // ������ ���� Ȯ�� ����
        }

        [Header("������ ���(���� �巡��)")]
        [SerializeField] private WeightedItem[] items;

        [Header("����")]
        [SerializeField] private float respawnTime = 5f;

        private Collider boxCollider;
        private Renderer[] renders;
        private bool isAvailable = true;

        private void Awake()
        {
            boxCollider = GetComponent<Collider>();
            renders = GetComponentsInChildren<Renderer>(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isAvailable) return;

            var pv = other.GetComponentInParent<PhotonView>();
            if (pv == null) return;

            if (!PhotonNetwork.IsMasterClient) return;

            // �÷��̾� �κ��丮 Ȯ��
            var inventory = other.GetComponentInParent<PlayerItemInventory>();
            if (inventory == null) return;

            if (inventory.HasItem) return;

            // ��÷
            int idx = DrawIndex();
            var prefab = items[idx].itemPrefab;
            if (prefab == null) return;

            photonView.RPC(nameof(RpcGiveItem), pv.Owner, prefab.name);

            StartCoroutine(ConsumeAndRespawn());
        }

        private int DrawIndex()
        {
            int total = items.Where(i => i.itemPrefab != null && i.weight > 0).Sum(i => i.weight);
            int roll = UnityEngine.Random.Range(0, total);
            int cum = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].itemPrefab == null || items[i].weight <= 0) continue;
                cum += items[i].weight;
                if (roll < cum) return i;
            }
            return 0;
        }

        [PunRPC]
        private void RpcGiveItem(string prefabName, PhotonMessageInfo info)
        {
            var all = items.Select(i => i.itemPrefab).Where(p => p != null).ToArray();
            var found = all.FirstOrDefault(p => p.name == prefabName);
            if (found == null)
            {
                found = Resources.Load<GameObject>(prefabName);
            }

            var player = PhotonView.Find(info.Sender.TagObject as int? ?? -1); 
            var inventory = FindObjectOfType<PlayerItemInventory>(); 
            
            if (inventory != null && found != null)
            {
                inventory.AssignItemPrefab(found);
            }
        }

        // ������ ������ ��Ȱ��ȭ ó����
        private IEnumerator ConsumeAndRespawn() 
        {
            SetActiveVisual(false); 
            isAvailable = false;

            yield return new WaitForSeconds(respawnTime);

            SetActiveVisual(true);
            isAvailable = true;
        }

        private void SetActiveVisual(bool active)
        {
            if (boxCollider != null) boxCollider.enabled = active;
            if (renders != null)
            {
                foreach (var r in renders) r.enabled = active;
            }
        }
    }
}
