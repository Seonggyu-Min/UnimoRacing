using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class GrapeItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private float invertDuration; 

        public void Use(GameObject owner)
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            var myView = owner.GetComponentInParent<PhotonView>();
            if (myView == null || !myView.IsMine)
            {
                Destroy(gameObject);
                return;
            }

            var candidates = FindObjectsOfType<DollyLaneSwitcher>(true)
                .Select(s => new { switcher = s, pv = s.GetComponent<PhotonView>() })
                .Where(x => x.pv != null && x.pv.Owner != null && x.pv.Owner.ActorNumber != myView.OwnerActorNr)
                .ToList();

            if (candidates.Count == 0)
            {
                Destroy(gameObject);
                return;
            }

            // 랜덤으로 한 명 선택
            int idx = Random.Range(0, candidates.Count);
            var target = candidates[idx];

            target.pv.RPC(nameof(DollyLaneSwitcher.RPCApplyInvertControls), target.pv.Owner, invertDuration);

            Destroy(gameObject);
        }
    }
}
