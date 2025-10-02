using Photon.Pun;
using UnityEngine;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhotonView))]
    public class VfxFollower : MonoBehaviourPun
    {
        private Transform target;
        private Vector3 localOffset;
        private bool alignToForward;

        // 모든 자식 파티클을 로컬 시뮬레이션 + 계층 스케일로 강제
        private void OnEnable()
        {
            var systems = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
        }

        [PunRPC]
        public void RpcSetupFollow(int targetViewId, float offsetY, bool align)
        {
            var pv = PhotonView.Find(targetViewId);
            if (pv == null) { Destroy(gameObject); return; }

            target = pv.transform;
            localOffset = new Vector3(0f, offsetY, 0f);
            alignToForward = align;

            // 부모-자식으로 묶고 시작 위치/회전 정렬
            transform.SetParent(target, worldPositionStays: false);
            transform.localPosition = localOffset;
            if (alignToForward)
                transform.rotation = Quaternion.LookRotation(target.forward, Vector3.up);
        }

        private void LateUpdate()
        {
            if (!target) { Destroy(gameObject); return; }

            // 혹시 이펙트 자체 로직이 transform을 건드려도 확실히 덮어쓰기
            transform.position = target.TransformPoint(localOffset);
            if (alignToForward)
                transform.rotation = Quaternion.LookRotation(target.forward, Vector3.up);
        }
    }
}
