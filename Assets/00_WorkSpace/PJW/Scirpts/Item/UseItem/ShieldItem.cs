using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class ShieldItem : MonoBehaviour, IUsableItem
    {
        [Header("����")]
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

            // ���� �ο�
            PlayerShield.Give(owner, shieldCount, durationSeconds);

            Destroy(gameObject);
        }
    }
}

/* ���� ȿ�� �ڵ�
if (PJW.PlayerShield.TryConsume(target))
{
    return;
}
*/
