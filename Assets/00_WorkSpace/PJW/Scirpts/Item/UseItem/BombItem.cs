using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class BombItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private float distanceAhead;

        public void Use(GameObject owner)
        {
            if (bombPrefab == null || owner == null)
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

            var units = cart.m_PositionUnits;
            float currentT = cart.m_Position;
            float targetT = currentT + distanceAhead;

            if(units == CinemachinePathBase.PositionUnits.Normalized)
            {
                if (path.Looped)
                    targetT = Mathf.Repeat(targetT, 1f);
                else
                    targetT = Mathf.Clamp01(targetT);
            }
            else 
            {
                float pathLen = path.PathLength;
                if (path.Looped)
                    targetT = Mathf.Repeat(targetT, pathLen);
                else
                    targetT = Mathf.Clamp(targetT, 0f, pathLen);
            }

            // 트랙 위 위치와 회전
            Vector3 spawnPos = path.EvaluatePositionAtUnit(targetT, units);
            Quaternion spawnRot = path.EvaluateOrientationAtUnit(targetT, units);

            Instantiate(bombPrefab, spawnPos, spawnRot);

            Destroy(gameObject);
        }
    }
}
