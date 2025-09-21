using Photon.Pun;
using UnityEngine;
using Cinemachine;
using YTW;

namespace PJW
{
    [DisallowMultipleComponent]
    public class VisionBlockItem : MonoBehaviour, IUsableItem
    {
        [Header("소환 설정")]
        [SerializeField] private string zoneResourceName = "VisionObscureZone"; // Resources 폴더 안에 프리팹 이름
        [SerializeField] private float distanceAhead = 0.02f; // 경로상 앞쪽 배치 거리 (정규화 경로 단위)

        [Header("사운드 키")]
        [SerializeField] private string sfxUseKey = "Blind_Use_SFX";

        public void Use(GameObject owner)
        {
            if (owner == null)
            {
                Debug.LogError("[VisionBlockItem] owner is null.");
                Destroy(gameObject);
                return;
            }

            var pv = owner.GetComponentInParent<PhotonView>();
            if (pv == null || !pv.IsMine)
            {
                Destroy(gameObject);
                return;
            }

            var cart = owner.GetComponentInParent<CinemachineDollyCart>();
            var path = cart ? cart.m_Path : null;
            if (cart == null || path == null)
            {
                Debug.LogError("[VisionBlockItem] DollyCart or Path not found.");
                Destroy(gameObject);
                return;
            }

            // 앞쪽 위치 계산
            float norm = cart.m_Position / path.PathLength;
            float spawnNorm = norm + distanceAhead;
            if (spawnNorm > 1f)
                spawnNorm -= 1f; // 루프 경로 고려

            Vector3 spawnPos = path.EvaluatePositionAtUnit(spawnNorm * path.PathLength, CinemachinePathBase.PositionUnits.Distance);
            Quaternion spawnRot = Quaternion.identity;

            // 프리팹 로드
            var prefab = Resources.Load<GameObject>(zoneResourceName);
            if (prefab == null)
            {
                Debug.LogError($"[VisionBlockItem] Prefab '{zoneResourceName}' not found in Resources.");
                Destroy(gameObject);
                return;
            }

            // 네트워크 전체에 소환
            PhotonNetwork.Instantiate(zoneResourceName, spawnPos, spawnRot);

            AudioManager.Instance.PlaySFX(sfxUseKey);

            Destroy(gameObject);
        }
    }
}
