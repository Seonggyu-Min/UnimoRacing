using UnityEngine;
using Photon.Pun;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class SwitchLockZone : MonoBehaviour
    {
        public enum Action { ApplyLock, RevertLock }

        [Header("부딪혔을 때 수행할 동작")]
        [SerializeField] private Action action = Action.ApplyLock;

        [Header("허용할 트랙 인덱스들(이 목록만 이동 가능)")]
        [SerializeField] private int[] allowedTracks = new int[] { };

        [Header("로컬 소유자만 처리 여부")]
        [SerializeField] private bool onlyLocalPlayer = true;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!TryGetController(other, out var ctrl)) return;

            if (action == Action.ApplyLock)
                Apply(ctrl);
            else
                Revert(ctrl);
        }

        private bool TryGetController(Collider other, out DollyCartController controller)
        {
            controller = other.GetComponentInParent<DollyCartController>();
            if (controller != null)
            {
                if (!onlyLocalPlayer) return true;

                var pv = controller.GetComponentInParent<PhotonView>() ?? controller.GetComponent<PhotonView>()
                         ?? other.GetComponentInParent<PhotonView>();
                return pv != null && pv.IsMine;
            }

            var rb = other.attachedRigidbody;
            if (rb != null)
            {
                controller = rb.GetComponentInParent<DollyCartController>() ?? rb.GetComponentInChildren<DollyCartController>();
                if (controller != null)
                {
                    if (!onlyLocalPlayer) return true;

                    var pv = controller.GetComponentInParent<PhotonView>() ?? controller.GetComponent<PhotonView>()
                             ?? rb.GetComponentInParent<PhotonView>() ?? rb.GetComponent<PhotonView>();
                    return pv != null && pv.IsMine;
                }
            }

            var root = rb ? rb.transform.root : other.transform.root;
            if (root != null)
            {
                controller = root.GetComponentInChildren<DollyCartController>(true);
                if (controller != null)
                {
                    if (!onlyLocalPlayer) return true;

                    var pv = controller.GetComponentInParent<PhotonView>() ?? controller.GetComponent<PhotonView>()
                             ?? root.GetComponentInChildren<PhotonView>(true);
                    return pv != null && pv.IsMine;
                }
            }

            controller = null;
            return false;
        }

        private void Apply(DollyCartController ctrl)
        {
            if (allowedTracks == null || allowedTracks.Length == 0) return;
            ctrl.LockAllExcept(allowedTracks);
        }

        private void Revert(DollyCartController ctrl)
        {
            if (allowedTracks == null || allowedTracks.Length == 0) return;
            ctrl.UnlockAllExcept(allowedTracks);
        }
    }
}
