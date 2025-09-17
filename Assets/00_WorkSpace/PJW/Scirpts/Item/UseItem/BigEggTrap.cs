using Cinemachine;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class BigEggTrap : MonoBehaviour, IUsableItem
    {
        [SerializeField] private GameObject trapPrefab;  
        [SerializeField] private float distanceBehind = 0.05f; // Normalized ¥‹¿ß

        public void Use(GameObject owner)
        {
            if (trapPrefab == null || owner == null)
            {
                Destroy(gameObject);
                return;
            }

            var cart = owner.GetComponentInParent<CinemachineDollyCart>();
            var path = cart != null ? cart.m_Path : null;
            if (cart == null || path == null)
            {
                Destroy(gameObject);
                return;
            }

            float currentT = cart.m_Position;
            float behindT = currentT - distanceBehind;
            if (path.Looped) behindT = Mathf.Repeat(behindT, 1f);
            else behindT = Mathf.Clamp01(behindT);

            Vector3 spawnPos = path.EvaluatePositionAtUnit(behindT, CinemachinePathBase.PositionUnits.Normalized);
            Quaternion spawnRot = path.EvaluateOrientationAtUnit(behindT, CinemachinePathBase.PositionUnits.Normalized);

            PhotonNetwork.InstantiateRoomObject(trapPrefab.name, spawnPos, spawnRot);

            Destroy(gameObject);
        }
    }
}
