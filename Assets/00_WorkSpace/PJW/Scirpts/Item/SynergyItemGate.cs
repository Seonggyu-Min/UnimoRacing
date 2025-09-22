using System.Collections;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    [DisallowMultipleComponent]
    public class SynergyItemGate : MonoBehaviour
    {
        [System.Serializable]
        public struct AllowedPair
        {
            public int characterId; // -1이면 아무 캐릭터나 허용
            public int kartId;      // -1이면 아무 카트나 허용
        }

        [Header("이 조합 중 하나라도 일치하면 로컬에 보이고/먹을 수 있음")]
        [SerializeField] private AllowedPair[] allowedPairs;

        [Header("컴포넌트 자동 검색")]
        [SerializeField] private bool autoFindTargets = true;

        [Header("제어 대상")]
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Collider[] targetColliders;
        [SerializeField] private GameObject[] extraObjects;

        private bool evaluated;

        private void Awake()
        {
            if (autoFindTargets)
            {
                targetRenderers = GetComponentsInChildren<Renderer>(true);
                targetColliders = GetComponentsInChildren<Collider>(true);
            }
        }

        private void OnEnable()
        {
            StartCoroutine(CoEvaluateAndApply());
        }

        private IEnumerator CoEvaluateAndApply()
        {
            // 로컬 내 레이서를 기다림
            PlayerRaceData local = null;
            while (local == null)
            {
                local = FindObjectsOfType<PlayerRaceData>(true)
                    .FirstOrDefault(r =>
                    {
                        var v = r.GetComponent<PhotonView>();
                        return v != null && v.IsMine;
                    });
                yield return null;
            }

            // PlayerRaceData 셋업 완료까지 대기(필요 시)
            while (!local.IsSetups)
                yield return null;

            bool allow = IsAllowedFor(local.CharacterID, local.KartID);
            ApplyVisibility(allow);
            evaluated = true;
        }

        private bool IsAllowedFor(int characterId, int kartId)
        {
            if (allowedPairs == null || allowedPairs.Length == 0) return true; // 비어있으면 모두 허용

            for (int i = 0; i < allowedPairs.Length; i++)
            {
                var p = allowedPairs[i];
                bool charOk = (p.characterId < 0) || (p.characterId == characterId);
                bool kartOk = (p.kartId < 0) || (p.kartId == kartId);
                if (charOk && kartOk) return true;
            }
            return false;
        }

        private void ApplyVisibility(bool allow)
        {
            if (targetRenderers != null)
            {
                foreach (var r in targetRenderers) if (r) r.enabled = allow;
            }
            if (targetColliders != null)
            {
                foreach (var c in targetColliders) if (c) c.enabled = allow;
            }
            if (extraObjects != null)
            {
                foreach (var go in extraObjects) if (go) go.SetActive(allow);
            }
        }

#if UNITY_EDITOR
        // 에디터에서 즉시 반영하고 싶다면 Gizmos 때도 평가
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying && !evaluated && autoFindTargets)
            {
                targetRenderers = GetComponentsInChildren<Renderer>(true);
                targetColliders = GetComponentsInChildren<Collider>(true);
            }
        }
#endif
    }
}
