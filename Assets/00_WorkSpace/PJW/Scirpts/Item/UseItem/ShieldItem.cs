using Photon.Pun;  
using UnityEngine;

namespace PJW
{
    public class ShieldItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private float shieldDuration = 5f;

        public void Use(GameObject owner)
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

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
