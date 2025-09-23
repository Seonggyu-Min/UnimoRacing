using System.Collections;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PJW
{
    [DisallowMultipleComponent]
    public class ItemUseUIButton : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button useButton;

        [SerializeField] private Image[] itemIcons = new Image[3];

        [Header("Debug")]
        [SerializeField] private bool logVerbose = true;

        private PlayerItemInventory boundInv;
        private Coroutine bindRoutine;

        private const string LogTag = "[ItemUseUIButton]";

        private void Awake()
        {
            if (useButton != null) useButton.onClick.AddListener(OnClickUse);

            SetUsable(false);
            HideAllIcons();
        }

        private void OnEnable()
        {
            if (bindRoutine == null) bindRoutine = StartCoroutine(AutoBindLocalInventory());
        }

        private void OnDisable()
        {
            if (bindRoutine != null)
            {
                StopCoroutine(bindRoutine);
                bindRoutine = null;
            }
            Unbind();
        }

        private IEnumerator AutoBindLocalInventory()
        {
            while (!PhotonNetwork.InRoom) yield return null;

            float time = 0f;
            const float timeout = 12f;
            const float interval = 0.25f;

            while (boundInv == null && time < timeout)
            {
                var inv = FindObjectsOfType<PlayerItemInventory>(true)
                    .FirstOrDefault(v =>
                    {
                        var pv = v.GetComponentInParent<PhotonView>() ?? v.GetComponent<PhotonView>();
                        return pv != null && pv.IsMine;
                    });

                if (inv != null)
                {
                    Bind(inv);
                    DumpUiStatus();
                    yield break;
                }

                yield return new WaitForSecondsRealtime(interval);
                time += interval;
            }

            DumpUiStatus();
            SetUsable(false);
            HideAllIcons();
        }

        private void Bind(PlayerItemInventory inv)
        {
            Unbind();

            boundInv = inv;
            boundInv.OnItemAssigned += OnItemAssigned;
            boundInv.OnItemAvailabilityChanged += OnItemAvailabilityChanged;

            // 안전하게 존재 여부 체크
            try
            {
                boundInv.OnItemCountChanged += OnItemCountChanged;
            }
            catch { /* 이전 버전일 수 있으니 무시 */ }

            // 초기 상태 반영
            OnItemAvailabilityChanged(boundInv.HasItem);

            var currentName = boundInv.CurrentItemName();
            if (!string.IsNullOrEmpty(currentName))
            {
                OnItemAssigned(currentName);
            }
            else
            {
                RefreshIcons();
            }
        }

        private void Unbind()
        {
            if (boundInv == null) return;
            boundInv.OnItemAssigned -= OnItemAssigned;
            boundInv.OnItemAvailabilityChanged -= OnItemAvailabilityChanged;
            try { boundInv.OnItemCountChanged -= OnItemCountChanged; } catch { }
            if (logVerbose) Debug.Log($"{LogTag} Unbound from inventory");
            boundInv = null;
        }

        private void OnClickUse()
        {
            if (boundInv == null) return;
            boundInv.UseItem();
            // 사용하면 맨 앞이 빠지므로 즉시 갱신
            RefreshIcons();
        }

        private void OnItemAssigned(string _)
        {
            // 맨 앞 아이템이 바뀔 수 있으니 전체 갱신
            RefreshIcons();
        }

        private void OnItemAvailabilityChanged(bool _)
        {
            RefreshIcons();
        }

        private void OnItemCountChanged(int _)
        {
            RefreshIcons();
        }

        private void RefreshIcons()
        {
            if (boundInv == null)
            {
                SetUsable(false);
                HideAllIcons();
                return;
            }

            // 버튼 활성화: 아이템 있고, 사용 가능할 때
            bool canUse = boundInv.HasItem && boundInv.CanUseItem;
            SetUsable(canUse);

            if (itemIcons == null || itemIcons.Length == 0)
            {
                return;
            }

            string[] names = boundInv.SnapshotItemNames(itemIcons.Length);

            for (int i = 0; i < itemIcons.Length; i++)
            {
                var img = itemIcons[i];
                if (img == null) continue;

                if (names != null && i < names.Length && !string.IsNullOrEmpty(names[i]))
                {
                    var sprite = ItemSpriteRegistry.Instance.GetIcon(names[i]);
                    img.sprite = sprite;
                    img.enabled = sprite != null;
                    img.raycastTarget = false;
                }
                else
                {
                    img.sprite = null;
                    img.enabled = false;
                    img.raycastTarget = false;
                }
            }
        }

        private void SetUsable(bool canUse)
        {
            if (useButton != null)
            {
                useButton.interactable = canUse;
                useButton.gameObject.SetActive(true);
            }
        }

        private void HideAllIcons()
        {
            if (itemIcons == null) return;
            foreach (var img in itemIcons)
            {
                if (img == null) continue;
                img.sprite = null;
                img.enabled = false;
                img.raycastTarget = false;
            }
        }

        [ContextMenu("Dump UI Status")]
        public void DumpUiStatus()
        {
            var es = EventSystem.current;

            Transform t = transform;
            int hop = 0;
            while (t != null && hop++ < 12)
            {
                var cg = t.GetComponent<CanvasGroup>();
                t = t.parent;
            }
        }
    }
}
