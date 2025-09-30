using UnityEngine;
using Photon.Pun;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class SwitchLockZone : MonoBehaviour
    {
        public enum Action { ApplyLock, RevertLock }

        [Header("부딪혔을 때 무엇을 할지")]
        [SerializeField] private Action action = Action.ApplyLock;     // 적용할 동작

        [Header("허용할 트랙 인덱스들(이 목록만 이동 가능)")]
        [SerializeField] private int[] allowedTracks = new int[] { 0, 1 };

        [Header("필터/네트워크 옵션")]
        [SerializeField] private string playerTag = "Player";         
        [SerializeField] private bool onlyLocalPlayer = true;     // 로컬 소유자만 처리

        private void Awake()
        {
            var col = GetComponent<Collider>();
        }

        private bool TryGetController(Collider other, out DollyCartController controller)
        {
            controller = null;

            if (!string.IsNullOrEmpty(playerTag))
            {
                bool tagOk = false;
                Transform t = other.transform;
                while (t != null)
                {
                    if (t.CompareTag(playerTag)) { tagOk = true; break; }
                    t = t.parent;
                }
                if (!tagOk) return false;
            }

            controller = other.GetComponentInParent<DollyCartController>();
            if (controller == null) return false;

            if (onlyLocalPlayer)
            {
                var pv = other.GetComponentInParent<PhotonView>();
                if (pv == null || !pv.IsMine) return false;
            }

            return true;
        }

        private void Apply(DollyCartController ctrl) => ctrl.LockAllExcept(allowedTracks);
        private void Revert(DollyCartController ctrl) => ctrl.UnlockAllExcept(allowedTracks);

        private void OnTriggerEnter(Collider other)
        {
            if (!TryGetController(other, out var ctrl)) return;

            if (action == Action.ApplyLock) Apply(ctrl);
            else Revert(ctrl);
        }
    }
}
