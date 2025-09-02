using Cinemachine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class BubbleItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private string PrefabName = "BubbleItem"; 
        [SerializeField] private float distanceAhead = 0.05f;

        public void Use(GameObject owner)
        {
            if (owner == null)
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
            float spawnT = currentT + distanceAhead;

            if (cart.m_Path.Looped)
            {
                if (spawnT > 1f) spawnT -= 1f;
            }
            else
            {
                spawnT = Mathf.Min(spawnT, 1f);
            }

            Vector3 spawnPos = cart.m_Path.EvaluatePositionAtUnit(
                spawnT, CinemachinePathBase.PositionUnits.Normalized);

            GameObject zone = PhotonNetwork.Instantiate(PrefabName, spawnPos, Quaternion.identity, 0, null);

            Destroy(gameObject);
        }
    }
}
