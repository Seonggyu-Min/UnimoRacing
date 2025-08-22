using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

namespace PJW
{
    /// <summary>
    /// 로컬 플레이어의 인벤토리를 찾아 아이템 사용 버튼을 자동으로 연결
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ItemUseUIButton : MonoBehaviour
    {
        [Tooltip("사용 버튼 (없으면 자동으로 자기 Button 컴포넌트 사용)")]
        [SerializeField] private Button useButton;

        private PlayerItemInventory inventory;

        private void Awake()
        {
            // 버튼이 지정되지 않았다면 자기 자신에서 찾아줌
            if (useButton == null)
                useButton = GetComponent<Button>();
        }

        private void Start()
        {
            // 플레이어 생성을 기다리기 위해 딜레이 후 인벤토리 찾기
            StartCoroutine(DelayedInit());
        }

        private IEnumerator DelayedInit()
        {
            yield return new WaitForSeconds(0.2f); // 플레이어 인스턴스화 시간 확보

            // 씬의 모든 인벤토리 중 IsMine인 것만 선택
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
                Debug.LogError("[ItemUseUIButton] 로컬 플레이어의 인벤토리를 찾을 수 없습니다.");
                useButton.interactable = false;
                yield break;
            }

            // 버튼 클릭 시 아이템 사용 연결
            useButton.onClick.AddListener(inventory.UseItem);

            // 초기 상태
            useButton.interactable = inventory.HasItem;

            // 인벤토리 상태가 바뀔 때마다 버튼 인터랙션 갱신
            inventory.OnItemAvailabilityChanged += hasItem =>
            {
                useButton.interactable = hasItem;
            };

            Debug.Log($"[ItemUseUIButton] 연결된 인벤토리: {inventory.name}");
        }
    }
}
