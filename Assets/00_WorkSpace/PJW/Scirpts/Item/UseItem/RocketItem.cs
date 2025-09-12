// PJW.RocketItem.cs
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class RocketItem : MonoBehaviour, IUsableItem
    {
        [Header("Resources 키(경로). 예: \"RocketProjectile\" 또는 \"Projectiles/RocketProjectile\"")]
        [SerializeField] private string rocketResourceKey = "RocketProjectile";

        [Header("스턴 시간(초)")]
        [SerializeField] private float stunDuration = 1.5f;

        [Header("발사 위치 오프셋 (오너 기준)")]
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;

        [Header("타겟 탐색 최대 거리")]
        [SerializeField] private float maxSearchDistance = 9999f;

        public void Use(GameObject owner)
        {
            // 오너/소유권 체크
            var ownerPv = owner ? owner.GetComponentInParent<PhotonView>() : null;
            if (ownerPv == null || !ownerPv.IsMine) { Destroy(gameObject); return; }

            // 타겟 찾기(상대만)
            var targetPv = FindNearestOpponent(owner.transform.position, ownerPv.ViewID, maxSearchDistance);
            if (targetPv == null) { Destroy(gameObject); return; }

            // Resources/룸 체크
            if (Resources.Load<GameObject>(rocketResourceKey) == null || !PhotonNetwork.InRoom)
            {
                Destroy(gameObject);
                return;
            }

            // 스폰 위치/회전
            Vector3 pos = owner.transform.TransformPoint(spawnOffset);
            Vector3 fwd = (targetPv.transform.position - pos).normalized;
            Quaternion rot = fwd.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(fwd, Vector3.up) : owner.transform.rotation;

            // 인스턴스 데이터: [0]=target ViewID, [1]=stunDuration
            object[] data = new object[] { targetPv.ViewID, stunDuration };
            PhotonNetwork.Instantiate(rocketResourceKey, pos, rot, 0, data);

            // 아이템 소비
            Destroy(gameObject);
        }

        private PhotonView FindNearestOpponent(Vector3 from, int myViewId, float maxDist)
        {
            PhotonView nearest = null;
            float bestSqr = maxDist * maxDist;

            foreach (var pv in FindObjectsOfType<PhotonView>())
            {
                if (pv == null) continue;
                if (pv.ViewID == myViewId) continue;   // 자기 자신 제외
                if (pv.Owner == null) continue;
                if (pv.IsMine) continue;               // 내 로컬 오브젝트 제외(상대만)

                var cart = pv.GetComponentInChildren<Cinemachine.CinemachineDollyCart>(true);
                if (cart == null) continue;

                float sqr = (pv.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    nearest = pv;
                }
            }
            return nearest;
        }
    }
}
