using Photon.Pun;
using UnityEngine;
using Cinemachine;

namespace PJW
{
    public class HoneyTrap : MonoBehaviour, IUsableItem
    {
        [SerializeField] private GameObject trapPrefab;     
        [SerializeField] private float distanceBehind = 0.05f;

        [SerializeField] private float slowMultiplier = 0.5f;
        [SerializeField] private float duration = 2.0f;

        public void Use(GameObject owner)
        {
            if (trapPrefab == null || owner == null)
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
            if (cart == null || cart.m_Path == null)
            {
                Destroy(gameObject);
                return;
            }

            float currentT = cart.m_Position;
            float behindT = currentT - distanceBehind;

            if (behindT < 0f && cart.m_Path.Looped)
                behindT += 1f;
            else
                behindT = Mathf.Max(behindT, 0f);

            Vector3 spawnPos = cart.m_Path.EvaluatePositionAtUnit(
                behindT, CinemachinePathBase.PositionUnits.Normalized);

            object[] data = new object[] { slowMultiplier, duration };
            PhotonNetwork.Instantiate(trapPrefab.name, spawnPos, Quaternion.identity, 0, data);

            Destroy(gameObject);
        }
    }
}
