using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhotonView))]
    public class NetworkVfxSpawner : MonoBehaviourPun
    {
        [Header("네트워크 옵션")]
        [SerializeField] private bool useBufferedRpc = false;   // 늦게 합류한 클라이언트도 보게 하려면 켜기 (AllBuffered)
        [SerializeField] private int attachRetryCount = 20;     // 타겟 PV 찾기 재시도 횟수 (0.1s * 20 = 최대 2초)
        [SerializeField] private float attachRetryInterval = 0.1f;

        // 모든 클라이언트에 월드 위치로 VFX 생성
        public void SpawnWorld(string resourcePath, Vector3 position, Quaternion rotation, float lifeSeconds = -1f)
        {
            var target = useBufferedRpc ? RpcTarget.AllBuffered : RpcTarget.All;
            photonView.RPC(nameof(RpcSpawnWorld), target, resourcePath, position, rotation, lifeSeconds);
        }

        // 모든 클라이언트에 대상 뷰ID에 부착하여 VFX 생성
        public void SpawnAttached(int targetViewId, string resourcePath, Vector3 localOffset, Vector3 localEuler, float lifeSeconds = -1f)
        {
            var target = useBufferedRpc ? RpcTarget.AllBuffered : RpcTarget.All;
            photonView.RPC(nameof(RpcSpawnAttached), target, targetViewId, resourcePath, localOffset, localEuler, lifeSeconds);
        }

        // ===== RPCs =====

        [PunRPC]
        private void RpcSpawnWorld(string resourcePath, Vector3 position, Quaternion rotation, float lifeSeconds)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[NetworkVfxSpawner] Not found: {resourcePath}");
                return;
            }

            var go = Instantiate(prefab, position, rotation);
            PlayAll(go);
            if (lifeSeconds > 0f) Destroy(go, lifeSeconds);
            else StartCoroutine(DestroyWhenFinished(go));
        }

        [PunRPC]
        private void RpcSpawnAttached(int targetViewId, string resourcePath, Vector3 localOffset, Vector3 localEuler, float lifeSeconds)
        {
            // 타이밍 문제 대비: 코루틴으로 잠깐 재시도
            StartCoroutine(SpawnAttachedWithRetry(targetViewId, resourcePath, localOffset, localEuler, lifeSeconds));
        }

        private IEnumerator SpawnAttachedWithRetry(int targetViewId, string resourcePath, Vector3 localOffset, Vector3 localEuler, float lifeSeconds)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[NetworkVfxSpawner] Not found: {resourcePath}");
                yield break;
            }

            PhotonView targetPv = null;
            for (int i = 0; i < attachRetryCount; i++)
            {
                targetPv = PhotonView.Find(targetViewId);
                if (targetPv != null) break;
                yield return new WaitForSeconds(attachRetryInterval);
            }

            if (targetPv == null)
            {
                Debug.LogWarning($"[NetworkVfxSpawner] Target PV not found after retry: {targetViewId}. Fallback to world spawn.");
                // 최후 수단: 월드에라도 보여주기
                var go0 = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                PlayAll(go0);
                if (lifeSeconds > 0f) Destroy(go0, lifeSeconds);
                yield break;
            }

            var go = Instantiate(prefab, targetPv.transform);
            go.transform.localPosition = localOffset;
            go.transform.localRotation = Quaternion.Euler(localEuler);

            PlayAll(go);
            if (lifeSeconds > 0f) Destroy(go, lifeSeconds);
            else StartCoroutine(DestroyWhenFinished(go));
        }

        // ===== 유틸 =====

        private static void PlayAll(GameObject root)
        {
            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems) ps.Play(true);
        }

        private static IEnumerator DestroyWhenFinished(GameObject root)
        {
            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            while (true)
            {
                bool anyAlive = false;
                foreach (var ps in systems)
                {
                    if (ps != null && ps.IsAlive(true)) { anyAlive = true; break; }
                }
                if (!anyAlive) break;
                yield return null;
            }
            if (root != null) Destroy(root);
        }
    }
}
