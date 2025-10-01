using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

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
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool onlyLocalPlayer = true;

        [Header("옵션")]
        [SerializeField] private bool useGlobalSwitchLock = false;

        private readonly HashSet<Collider> _collidersInZone = new HashSet<Collider>();

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (!col.isTrigger)
            {
                Debug.LogWarning($"[SwitchLockZone] {gameObject.name}의 Collider는 Is Trigger가 체크되어야 합니다.", gameObject);
            }
        }

        private bool TryGetController(Collider other, out DollyCartController controller)
        {
            controller = null;

            if (!string.IsNullOrEmpty(playerTag) && !other.transform.root.CompareTag(playerTag))
            {
                return false;
            }

            controller = other.GetComponentInParent<DollyCartController>();
            if (controller == null) return false;

            if (onlyLocalPlayer)
            {
                var pv = controller.GetComponent<PhotonView>(); 
                if (pv == null || !pv.IsMine) return false;
            }

            return true;
        }

        // Apply/​Revert를 LockAllExcept 대신 파티션 API로 전환
        private void Apply(DollyCartController ctrl)  
        {
            ctrl.SetPartition(allowedTracks);         // A집합 설정
        }
        private void Revert(DollyCartController ctrl) // 
        {
            ctrl.ClearPartition();                    // A<->B 차단 규칙 해제
        }

        //private void Apply(DollyCartController ctrl) => ctrl.LockAllExcept(allowedTracks);
        //private void Revert(DollyCartController ctrl) => ctrl.UnlockAllExcept(allowedTracks);

        private void OnTriggerEnter(Collider other)
        {
            // 이미 존에 진입한 콜라이더는 무시하여 중복 실행 방지
            if (_collidersInZone.Contains(other)) return;

            if (!TryGetController(other, out var ctrl)) return;

            // 중복 방지를 위해 Set에 콜라이더 추가
            _collidersInZone.Add(other);

            Debug.Log($"[Zone]: 진입 {action}, 허용=[{string.Join(",", allowedTracks)}]");
            if (action == Action.ApplyLock) Apply(ctrl);
            else Revert(ctrl);
        }

        // 존을 나갈 때 락을 대칭적으로 해제하기 위한 OnTriggerExit 구현
        private void OnTriggerExit(Collider other)
        {
            if (!_collidersInZone.Contains(other)) return;
            _collidersInZone.Remove(other);
        }

        private void OnDisable() { ForceCleanup(); }
        private void OnDestroy() { ForceCleanup(); }

        private void ForceCleanup()
        {
            foreach (var col in _collidersInZone)
            {
                if (col == null) continue;
                if (TryGetController(col, out var ctrl))
                {
                    // Enter 때 했던 동작의 반대(대칭)로 정리
                    if (action == Action.ApplyLock) Revert(ctrl);
                    else Apply(ctrl);
                    if (useGlobalSwitchLock) ctrl.RemoveSwitchLock();
                }
            }
            _collidersInZone.Clear();
        }
    }
}
