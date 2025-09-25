using MSG;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YTW;

namespace PJW
{
    public interface IUsableItem { void Use(GameObject owner); }

    [DisallowMultipleComponent]
    public class PlayerItemInventory : MonoBehaviour
    {
        [SerializeField] private int capacity = 3;   // 최대 보유 개수 (기본 3)

        [Header("사운드 키")]
        [SerializeField] private string sfxHitKey = "Lock_SFX"; // 잠금 시 효과음

        private PhotonView ownerView;

        // 아이템 큐: 먼저 먹은 아이템을 먼저 사용(FIFO)
        private readonly Queue<GameObject> items = new Queue<GameObject>();

        public bool HasItem => items.Count > 0;
        public bool CanUseItem { get; private set; } = true;
        public bool IsFull => items.Count >= capacity;
        public int Count => items.Count;

        // 비었는지 여부
        public bool IsEmpty() => !HasItem;

        // 기존 UI 호환 이벤트
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
        /// 프리팹 이름으로 아이템을 추가
        /// </summary>
        public void AssignItemByPrefabName(string prefabName)
        {
            var prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                Debug.LogError($"No prefab: {prefabName}");
                return;
            }
            AssignItemPrefab(prefab);
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

            // 아이템 사용 미션 증가 처리
            if (MissionService.Instance != null)
            {
                MissionService.Instance.Report(MissionVerb.Use, MissionObject.Item, false, 1);
            }

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

            var pv = ownerView ?? GetComponent<PhotonView>() ?? GetComponentInParent<PhotonView>();
            if (pv != null)
            {
                var spawner = pv.GetComponent<NetworkVfxSpawner>() ?? pv.gameObject.AddComponent<NetworkVfxSpawner>();

                string vfxPath = $"VFX/Items/{currentItemPrefab.name}_Use";

                // 소유자 기준 뒤쪽 -1m에 부착, 2초 후 파괴 
                spawner.SpawnAttached(pv.ViewID, vfxPath, new Vector3(0f, 0f, -1f), Vector3.zero, 2f);
            }

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

            // 잠금 사운드
            AudioManager.Instance.PlaySFX(sfxHitKey);
        }

        private IEnumerator LockRoutine(float duration)
        {
            CanUseItem = false;
            OnItemAvailabilityChanged?.Invoke(HasItem);
            yield return new WaitForSecondsRealtime(duration);
            CanUseItem = true;
            lockRoutine = null;
            OnItemAvailabilityChanged?.Invoke(HasItem);
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

        /// <summary>
        /// 현재 큐 상태를 배열로 반환 (UI 등에 표시용)
        /// </summary>
        public string[] SnapshotItemNames(int maxCount = 3)
        {
            if (maxCount <= 0) return Array.Empty<string>();
            return items.Take(Mathf.Min(maxCount, items.Count))
                        .Select(go => go != null ? go.name : null)
                        .ToArray();
        }
    }
}
