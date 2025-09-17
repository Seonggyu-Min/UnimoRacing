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
        [SerializeField] private Image itemIcon;

        [Header("Debug")]
        [SerializeField] private bool logVerbose = true;

        private PlayerItemInventory boundInv;
        private Coroutine bindRoutine;

        private const string LogTag = "[ItemUseUIButton]";

        private void Awake()
        {
            if (useButton != null) useButton.onClick.AddListener(OnClickUse);

            SetUsable(false);
            SetIconVisible(false);
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
            SetIconVisible(false);
        }

        private void Bind(PlayerItemInventory inv)
        {
            Unbind();

            boundInv = inv;
            boundInv.OnItemAssigned += OnItemAssigned;
            boundInv.OnItemAvailabilityChanged += OnItemAvailabilityChanged;


            OnItemAvailabilityChanged(boundInv.HasItem);

            var currentName = boundInv.CurrentItemName();
            if (!string.IsNullOrEmpty(currentName))
            {
                OnItemAssigned(currentName);
            }
        }

        private void Unbind()
        {
            if (boundInv == null) return;
            boundInv.OnItemAssigned -= OnItemAssigned;
            boundInv.OnItemAvailabilityChanged -= OnItemAvailabilityChanged;
            if (logVerbose) Debug.Log($"{LogTag} Unbound from inventory");
            boundInv = null;
        }

        private void OnClickUse()
        {
            boundInv.UseItem();
        }

        private void OnItemAssigned(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                // 아이템 없을 때 아이콘 숨김
                if (itemIcon != null)
                {
                    itemIcon.sprite = null;
                    SetIconVisible(false);
                }
                return;
            }

            var sprite = ItemSpriteRegistry.Instance.GetIcon(prefabName);
            if (itemIcon != null)
            {
                itemIcon.sprite = sprite;
                SetIconVisible(sprite != null);
            }
        }

        private void OnItemAvailabilityChanged(bool hasItem)
        {
            bool canUse = hasItem && boundInv != null && boundInv.CanUseItem;
            SetUsable(canUse);

            // 아이템 없으면 아이콘도 숨기기 (이 부분 추가!)
            if (!hasItem && itemIcon != null)
            {
                itemIcon.sprite = null;
                SetIconVisible(false);
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

        private void SetIconVisible(bool visible)
        {
            if (itemIcon != null)
            {
                itemIcon.enabled = visible;
                itemIcon.raycastTarget = false; 
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
