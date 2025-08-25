using Photon.Pun;
using UnityEngine;

namespace PJW
{
    [DisallowMultipleComponent]
    public class VisionBlockItem : MonoBehaviour, IUsableItem
    {
        [Header("Obscure Settings")]
        [SerializeField] private float blockDuration = 3f;     
        [SerializeField] private float fadeIn = 0.2f;          
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.9f; 
        [SerializeField] private float fadeOut = 0.4f;         

        public void Use(GameObject owner)
        {
            if (owner == null)
            {
                Debug.LogError("[VisionBlockItem] owner is null.");
                Destroy(gameObject);
                return;
            }

            var ownerView = owner.GetComponent<PhotonView>() ?? owner.GetComponentInParent<PhotonView>();
            if (ownerView == null)
            {
                Debug.LogError("[VisionBlockItem] Owner has no PhotonView to send RPC.");
                Destroy(gameObject);
                return;
            }

            if (!ownerView.IsMine)
            {
                Destroy(gameObject);
                return;
            }

            ownerView.RPC(
                "RpcObscureOpponents",
                RpcTarget.All,
                ownerView.OwnerActorNr,
                blockDuration,
                fadeIn,
                maxAlpha,
                fadeOut
            );

            Destroy(gameObject);
        }
    }
}
