using Cinemachine;
using Photon.Pun;
using UnityEngine;
using YTW;

namespace PJW
{
    [DisallowMultipleComponent]
    public class BombItem : MonoBehaviour, IUsableItem
    {
        [Header("Resources")]
        [SerializeField] private string bombResourceKey = "BombTrap";

        [Header("트랙 기준 착지 위치 (경로 앞쪽 거리)")]
        [SerializeField] private float distanceAhead = 0.02f;

        [Header("던지기 연출")]
        [Tooltip("던지는 시작 높이")]
        [SerializeField] private float headHeight = 1.2f;
        [Tooltip("포물선 최고 높이")]
        [SerializeField] private float arcHeight = 1.0f;
        [Tooltip("비행 시간")]
        [SerializeField] private float flightTime = 0.6f;
        [Tooltip("던지기 비주얼 프리팹")]
        [SerializeField] private GameObject throwVisualPrefab;
        [Tooltip("비주얼 위치 보정")]
        [SerializeField] private Vector3 visualOffset = Vector3.zero;
        [Tooltip("진행 방향을 바라보게 할지 여부")]
        [SerializeField] private bool visualLookForward = true;

        [Header("사운드")]
        [SerializeField] private string sfxUseKey = "BombItem_Use";

        public void Use(GameObject owner)
        {
            if (!ValidateOwner(owner, out var ownerPv, out var cart, out var path))
            {
                Destroy(gameObject);
                return;
            }

            var units = cart.m_PositionUnits;
            float currentT = cart.m_Position;
            float targetT = currentT + distanceAhead;

            if (units == CinemachinePathBase.PositionUnits.Normalized)
                targetT = path.Looped ? Mathf.Repeat(targetT, 1f) : Mathf.Clamp01(targetT);
            else
            {
                float len = path.PathLength;
                targetT = path.Looped ? Mathf.Repeat(targetT, len) : Mathf.Clamp(targetT, 0f, len);
            }

            // 시작/착지 좌표 & 착지 회전(트랙 정렬)
            Vector3 startOnTrack = path.EvaluatePositionAtUnit(currentT, units);
            Vector3 startPos = startOnTrack + Vector3.up * headHeight; // 머리 위에서 시작
            Vector3 endPos = path.EvaluatePositionAtUnit(targetT, units);
            Quaternion endRot = path.EvaluateOrientationAtUnit(targetT, units);

            if (!string.IsNullOrEmpty(sfxUseKey))
                AudioManager.Instance.PlaySFX(sfxUseKey);

            StartCoroutine(ThrowAndPlace(startPos, endPos, endRot));

        }

        private System.Collections.IEnumerator ThrowAndPlace(Vector3 startPos, Vector3 endPos, Quaternion endRot)
        {
            // 로컬 비주얼 생성(선택)
            GameObject visual = null;
            if (throwVisualPrefab != null)
            {
                visual = Instantiate(throwVisualPrefab, startPos + visualOffset, Quaternion.identity);
            }

            float t = 0f;
            Vector3 lastPos = startPos;

            while (t < flightTime)
            {
                float u = t / flightTime;                // 0 → 1
                Vector3 flat = Vector3.Lerp(startPos, endPos, u);
                float height = 4f * arcHeight * u * (1f - u); 
                Vector3 pos = flat + Vector3.up * height;

                if (visual != null)
                {
                    visual.transform.position = pos + visualOffset;
                    if (visualLookForward)
                    {
                        Vector3 vel = (pos - lastPos);
                        if (vel.sqrMagnitude > 0.0001f)
                            visual.transform.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up);
                    }
                }

                lastPos = pos;
                t += Time.deltaTime;
                yield return null;
            }

            // 착지 스냅(마지막 프레임 보정)
            if (visual != null)
            {
                visual.transform.position = endPos + visualOffset;
                visual.transform.rotation = endRot;
            }

            // 네트워크로 실제 트랩 설치
            if (PhotonNetwork.InRoom && !string.IsNullOrEmpty(bombResourceKey))
            {
                PhotonNetwork.Instantiate(bombResourceKey, endPos, endRot);
            }

            // 로컬 비주얼 정리
            if (visual != null)
                Destroy(visual);

            // 아이템 프리팹 제거(Use 종료)
            Destroy(gameObject);
        }

        private bool ValidateOwner(GameObject owner, out PhotonView ownerPv, out CinemachineDollyCart cart, out CinemachinePathBase path)
        {
            ownerPv = null;
            cart = null;
            path = null;

            if (owner == null) return false;

            ownerPv = owner.GetComponentInParent<PhotonView>();
            if (ownerPv == null || !ownerPv.IsMine) return false;

            cart = owner.GetComponentInParent<CinemachineDollyCart>();
            path = cart ? cart.m_Path : null;
            if (cart == null || path == null) return false;

            return true;
        }
    }
}
