using Photon.Pun;  
using UnityEngine;
using YTW;

namespace PJW
{
    public class ShieldItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private float shieldDuration = 5f;

        [SerializeField] private string sfxUseKey = "Shield_Use";   
        [SerializeField] private string sfxLoopKey = "Shield";

        public void Use(GameObject owner)
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            AudioManager.Instance.PlaySFX(sfxUseKey);

            var shield = owner.GetComponentInParent<PlayerShield>();
            if (shield == null)
            {
                shield = owner.AddComponent<PlayerShield>();
            }

            var pv = owner.GetComponentInParent<PhotonView>();
            if (pv != null)
            {
                pv.RPC(nameof(PlayerShield.RpcActivateShield), RpcTarget.All, shieldDuration);
            }
            else
            {
                shield.ActivateShield(shieldDuration);
            }

            Destroy(gameObject);
        }
    }
}
