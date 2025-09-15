using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public interface IUsableItem { void Use(GameObject owner); }

    public class PlayerItemInventory : MonoBehaviour
    {
        [SerializeField] private int capacity = 3;   // 최대 보유 개수 (기본 3)

        private PhotonView ownerView;

        // 아이템 큐: 먼저 먹은 아이템을 먼저 사용(FIFO)
        private readonly Queue<GameObject> items = new Queue<GameObject>();

        public bool HasItem => items.Count > 0;
        public bool CanUseItem { get; private set; } = true;
        public bool IsFull => items.Count >= capacity;
        public int Count => items.Count;

        // 기존 UI 호환을 위해 이름 유지
        public event Action<bool> OnItemAvailabilityChanged; // 비었는지 여부
        public event Action<string> OnItemAssigned;          // 현재(맨 앞) 아이템 이름 알림
        public event Action<int> OnItemCountChanged;         // 총 개수 변경 알림(추가됨)

        private Coroutine lockRoutine;

        private void Awake()
        {
            if (ownerView == null)
                ownerView = GetComponent<PhotonView>() ?? GetComponentInParent<PhotonView>();
        }

        /// <summary>
        /// 아이템을 인벤토리에 추가 (가득 차면 무시)
        /// </summary>
        public void AssignItemPrefab(GameObject itemPrefab)
        {
            if (itemPrefab == null) return;
            if (IsFull) return; // 가득 찼으면 더 이상 담지 않음(원하면 교체 로직으로 바꿔도 됨)

            items.Enqueue(itemPrefab);

            // 현재(맨 앞) 아이템 이름 전달
            OnItemAssigned?.Invoke(CurrentItemName());

            // 보유 여부/개수 갱신
            OnItemAvailabilityChanged?.Invoke(HasItem);
            OnItemCountChanged?.Invoke(items.Count);
        }

        /// <summary>
        /// 가장 앞의 아이템 사용
        /// </summary>
        public void UseItem()
        {
            if (!CanUseItem) return;
            if (ownerView != null && !ownerView.IsMine) return;
            if (!HasItem) return;

            var currentItemPrefab = items.Peek();
            if (currentItemPrefab == null)
            {
                // 방어적 처리: 잘못 들어온 null은 버림
                items.Dequeue();
                FireChangedEvents();
                return;
            }

            var go = Instantiate(currentItemPrefab, transform.position, Quaternion.identity);
            go.name = $"{currentItemPrefab.name}_Inst";
            var usable = go.GetComponent<IUsableItem>();

            if (usable == null)
            {
                Destroy(go);
                // 아이템 자체가 잘못된 경우도 소비만 진행(막히지 않도록)
                items.Dequeue();
                FireChangedEvents();
                return;
            }

            usable.Use(ownerView != null ? ownerView.gameObject : gameObject);

            // 사용 완료 → 큐에서 제거
            items.Dequeue();

            FireChangedEvents();
        }

        /// <summary>
        /// 현재(맨 앞) 아이템 이름 반환. 없으면 null
        /// </summary>
        public string CurrentItemName()
        {
            return HasItem ? items.Peek().name : null;
        }

        /// <summary>
        /// 일정 시간 동안 사용 잠금
        /// </summary>
        public void ApplyItemLock(float duration)
        {
            if (lockRoutine != null) StopCoroutine(lockRoutine);
            lockRoutine = StartCoroutine(LockRoutine(duration));
        }

        [PunRPC]
        private void RPCApplyItemLock(float duration)
        {
            var myInv = FindObjectsOfType<PlayerItemInventory>(true)
                .FirstOrDefault(inv =>
                {
                    var v = inv.ownerView ?? inv.GetComponent<PhotonView>() ?? inv.GetComponentInParent<PhotonView>();
                    return v != null && v.IsMine;
                });

            if (myInv != null) myInv.ApplyItemLock(duration);
            else ApplyItemLock(duration);
        }

        private IEnumerator LockRoutine(float duration)
        {
            CanUseItem = false;
            yield return new WaitForSeconds(duration);
            CanUseItem = true;
            lockRoutine = null;
        }

        [PunRPC]
        private void RpcObscureOpponents(int ownerActorNr, float duration, float fadeIn, float maxAlpha, float fadeOut)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == ownerActorNr) return;
            VisionObscureController.EnsureInScene().Obscure(duration, fadeIn, maxAlpha, fadeOut);
        }

        /// <summary>
        /// 전체 초기화가 필요할 때 사용(현재는 내부용)
        /// </summary>
        private void ClearAll()
        {
            items.Clear();
            FireChangedEvents();
        }

        /// <summary>
        /// 이벤트들을 현재 상태에 맞춰 한 번에 쏴줌
        /// </summary>
        private void FireChangedEvents()
        {
            OnItemAssigned?.Invoke(CurrentItemName());
            OnItemAvailabilityChanged?.Invoke(HasItem);
            OnItemCountChanged?.Invoke(items.Count);
        }

        public string[] SnapshotItemNames(int maxCount = 3)
        {
            if (maxCount <= 0) return Array.Empty<string>();
            return items.Take(Mathf.Min(maxCount, items.Count))
                        .Select(go => go != null ? go.name : null)
                        .ToArray();
        }
    }
}
