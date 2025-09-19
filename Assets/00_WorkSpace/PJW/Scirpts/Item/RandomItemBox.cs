using Photon.Pun;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using YTW;

namespace PJW
{
    [DisallowMultipleComponent]
    public class RandomItemBox : MonoBehaviourPun
    {
        [Serializable]
        private class WeightedItem
        {
            public GameObject itemPrefab;
            public Sprite icon;
            public int weight = 1; // 아이템 등장 확률 조정
        }

        [Header("아이템 목록")]
        [SerializeField] private WeightedItem[] items;

        [Header("설정")]
        [SerializeField] private float respawnTime;

        [Header("회전 설정")]
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 60f, 0f);

        [Header("사운드 키")]
        [SerializeField] private string sfxGetKey = "Itembox_SFX";      // 아이템 획득 (2D)

        private Collider boxCollider;
        private Renderer[] renders;
        private bool isAvailable = true;
        private Coroutine respawnCO;

        private void Awake()
        {
            boxCollider = GetComponent<Collider>();
            renders = GetComponentsInChildren<Renderer>(true);

            // 아이콘 레지스트리 등록
            foreach (var it in items)
            {
                if (it != null && it.itemPrefab != null && it.icon != null)
                {
                    ItemSpriteRegistry.Instance.RegisterIcon(it.itemPrefab.name, it.icon);
                }
            }
        }

        private void Update()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isAvailable) return;

            var pv = other.GetComponentInParent<PhotonView>();
            if (pv == null) return;

            if (!PhotonNetwork.IsMasterClient) return;

            // 플레이어 인벤토리 확인
            var inventory = other.GetComponentInParent<PlayerItemInventory>();
            if (inventory == null) return;

            // 가득 차 있으면 지급하지 않음
            if (inventory.IsFull) return;

            // 추첨
            int idx = DrawIndex();
            var prefab = items[idx].itemPrefab;
            if (prefab == null) return;


            // 소유자 클라이언트의 인벤토리에 아이템 지급
            photonView.RPC(nameof(RpcGiveItem), pv.Owner, prefab.name);

            // 상자 소비 & 리스폰
            if (respawnCO != null)
            {
                StopCoroutine(respawnCO);
                respawnCO = null;
            }
            respawnCO = StartCoroutine(ConsumeAndRespawn());
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

            var allInventories = FindObjectsOfType<PlayerItemInventory>();
            PlayerItemInventory myInventory = null;

            foreach (var inv in allInventories)
            {
                var pv = inv.GetComponent<PhotonView>() ?? inv.GetComponentInParent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    myInventory = inv;
                    break;
                }
            }

            if (myInventory != null && found != null)
            {
                myInventory.AssignItemPrefab(found);

                if (!string.IsNullOrWhiteSpace(sfxGetKey) && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(sfxGetKey);
                }
            }
        }

        // 아이템 먹을시 비활성화 처리함
        private IEnumerator ConsumeAndRespawn()
        {
            if (!PhotonNetwork.IsMasterClient) yield break; 
            photonView.RPC(nameof(RPCSetActiveVisualRandomBox), RpcTarget.All, false); // 박스 비활성화 전파
            yield return new WaitForSeconds(respawnTime);
            photonView.RPC(nameof(RPCSetActiveVisualRandomBox), RpcTarget.All, true);  // 박스 활성화 전파
        }

        [PunRPC]
        private void RPCSetActiveVisualRandomBox(bool active)
        {
            isAvailable = active;
            if (boxCollider != null) boxCollider.enabled = active;
            if (renders != null)
            {
                foreach (var r in renders) r.enabled = active;
            }
        }
    }
}
