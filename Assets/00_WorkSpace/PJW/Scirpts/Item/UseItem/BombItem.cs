// Assets/00_WorkSpace/PJW/Scirpts/Item/UseItem/BombItem.cs
using Cinemachine;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class BombItem : MonoBehaviour, IUsableItem
    {
        [Header("Resources")]
        [SerializeField] private string bombResourceKey = "BombTrap";

        [Header("앞쪽 배치 거리")]
        [SerializeField] private float distanceAhead = 0.02f;

        public void Use(GameObject owner)
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            var ownerPv = owner.GetComponentInParent<PhotonView>();
            if (ownerPv == null || !ownerPv.IsMine)
            {
                Destroy(gameObject);
                return;
            }

            var cart = owner.GetComponentInParent<CinemachineDollyCart>();
            var path = cart ? cart.m_Path : null;
            if (cart == null || path == null)
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

            Vector3 spawnPos = path.EvaluatePositionAtUnit(targetT, units);
            Quaternion spawnRot = path.EvaluateOrientationAtUnit(targetT, units);

            var probe = Resources.Load<GameObject>(bombResourceKey);
            if (probe == null || !PhotonNetwork.InRoom)
                return;

            PhotonNetwork.Instantiate(bombResourceKey, spawnPos, spawnRot);
            Destroy(gameObject);
        }
    }
}
