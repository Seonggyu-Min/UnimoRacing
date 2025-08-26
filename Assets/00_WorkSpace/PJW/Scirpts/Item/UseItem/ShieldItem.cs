using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class ShieldItem : MonoBehaviour, IUsableItem
    {
        [Header("설정")]
        [SerializeField] private int shieldCount = 1;        
        [SerializeField] private float durationSeconds = 0f; 

        public void Use(GameObject owner)
        {
            if (owner == null) { Destroy(gameObject); return; }

            var ownerView = owner.GetComponent<PhotonView>() ?? owner.GetComponentInParent<PhotonView>();
            if (ownerView != null && !ownerView.IsMine)
            {
                Destroy(gameObject);
                return;
            }

            // 방패 부여
            PlayerShield.Give(owner, shieldCount, durationSeconds);

            Destroy(gameObject);
        }
    }
}

/* 방패 효과 코드
if (PJW.PlayerShield.TryConsume(target))
{
    return;
}
*/
