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

            if (logVerbose) Debug.Log($"{LogTag} Awake. useButton={(useButton != null)} itemIcon={(itemIcon != null)}");
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
            if (logVerbose) Debug.Log($"{LogTag} AutoBindLocalInventory start. InRoom={PhotonNetwork.InRoom}");

            // 방 입장 대기
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
                    if (logVerbose) Debug.Log($"{LogTag} Found local inventory: {inv.name}");
                    Bind(inv);
                    DumpUiStatus();
                    yield break;
                }

                if (logVerbose) Debug.Log($"{LogTag} Local inventory not yet spawned… retrying");
                yield return new WaitForSecondsRealtime(interval);
                time += interval;
            }

            Debug.LogWarning($"{LogTag} FAILED to find local inventory in {timeout}s");
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

            if (logVerbose)
            {
                Debug.Log($"{LogTag} Bound to inventory. HasItem={boundInv.HasItem} CanUseItem={boundInv.CanUseItem}");
            }

            // 초기 상태 반영
            OnItemAvailabilityChanged(boundInv.HasItem);

            var currentName = boundInv.CurrentItemName();
            if (!string.IsNullOrEmpty(currentName))
            {
                if (logVerbose) Debug.Log($"{LogTag} Initial item = {currentName}");
                OnItemAssigned(currentName);
            }
            else
            {
                if (logVerbose) Debug.Log($"{LogTag} Initial item = (none)");
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
            if (boundInv == null)
            {
                Debug.LogWarning($"{LogTag} Click ignored: inventory not bound");
                return;
            }
            if (!boundInv.CanUseItem)
            {
                Debug.LogWarning($"{LogTag} Click ignored: CanUseItem=false");
                return;
            }

            if (logVerbose) Debug.Log($"{LogTag} Use clicked → calling boundInv.UseItem()");
            boundInv.UseItem();
        }

        private void OnItemAssigned(string prefabName)
        {
            if (logVerbose) Debug.Log($"{LogTag} OnItemAssigned: {prefabName}");

            var sprite = ItemSpriteRegistry.Instance.GetIcon(prefabName);
            if (itemIcon != null)
            {
                itemIcon.sprite = sprite;
                SetIconVisible(sprite != null);
                if (sprite == null)
                    Debug.LogWarning($"{LogTag} Sprite NOT found in ItemSpriteRegistry for '{prefabName}'");
                else if (logVerbose)
                    Debug.Log($"{LogTag} Sprite applied: {sprite.name}");
            }
        }

        private void OnItemAvailabilityChanged(bool hasItem)
        {
            bool canUse = hasItem && boundInv != null && boundInv.CanUseItem;
            SetUsable(canUse);

            if (logVerbose)
            {
                Debug.Log($"{LogTag} OnItemAvailabilityChanged: hasItem={hasItem} CanUseItem={(boundInv != null && boundInv.CanUseItem)} → interactable={canUse}");
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
                itemIcon.raycastTarget = false; // 클릭 방해 방지
            }
        }

        [ContextMenu("Dump UI Status")]
        public void DumpUiStatus()
        {
            var es = EventSystem.current;
            Debug.Log(
                $"{LogTag} Dump\n" +
                $"- gameObject.activeInHierarchy={gameObject.activeInHierarchy}\n" +
                $"- useButton.exists={(useButton != null)} active={(useButton != null && useButton.gameObject.activeInHierarchy)} interactable={(useButton != null && useButton.interactable)}\n" +
                $"- itemIcon.exists={(itemIcon != null)} enabled={(itemIcon != null && itemIcon.enabled)} sprite={(itemIcon != null ? (itemIcon.sprite != null ? itemIcon.sprite.name : "null") : "n/a")}\n" +
                $"- EventSystem.exists={(es != null)}\n" +
                $"- Time.timeScale={Time.timeScale}"
            );

            // 부모 CanvasGroup 체크(버튼이 눌리지 않는 흔한 원인)
            Transform t = transform;
            int hop = 0;
            while (t != null && hop++ < 12)
            {
                var cg = t.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    Debug.Log($"{LogTag} CanvasGroup on '{t.name}': interactable={cg.interactable} blocksRaycasts={cg.blocksRaycasts} alpha={cg.alpha}");
                }
                t = t.parent;
            }
        }
    }
}
