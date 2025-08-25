using Photon.Pun;
using UnityEngine;

namespace PJW
{
    /// <summary>
    /// 사용 시 상대 화면을 일정 시간 가리는 아이템.
    /// RPC는 플레이어의 PhotonView를 통해 전송한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class VisionBlockItem : MonoBehaviour, IUsableItem
    {
        [Header("Obscure Settings")]
        [SerializeField] private float blockDuration = 3f;      // 유지 시간
        [SerializeField] private float fadeIn = 0.2f;           // 페이드 인 시간
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.9f; // 최대 불투명도
        [SerializeField] private float fadeOut = 0.4f;          // 페이드 아웃 시간

        public void Use(GameObject owner)
        {
            if (owner == null)
            {
                Debug.LogError("[VisionBlockItem] owner is null.");
                Destroy(gameObject);
                return;
            }

            // 아이템을 사용한 '플레이어'의 PhotonView
            var ownerView = owner.GetComponent<PhotonView>() ?? owner.GetComponentInParent<PhotonView>();
            if (ownerView == null)
            {
                Debug.LogError("[VisionBlockItem] Owner has no PhotonView to send RPC.");
                Destroy(gameObject);
                return;
            }

            // 내 소유일 때만 브로드캐스트
            if (!ownerView.IsMine)
            {
                Destroy(gameObject);
                return;
            }

            // 플레이어의 PhotonView로 RPC 호출 (PlayerEffectRpc가 같은 오브젝트에 있어야 함)
            ownerView.RPC(
                "RpcObscureOpponents",
                RpcTarget.All,
                ownerView.OwnerActorNr,
                blockDuration,
                fadeIn,
                maxAlpha,
                fadeOut
            );

            // 소모형 아이템이라 즉시 파괴
            Destroy(gameObject);
        }
    }
}
