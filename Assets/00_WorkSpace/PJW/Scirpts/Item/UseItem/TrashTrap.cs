using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PJW
{
    public class TrashTrap : MonoBehaviour, IUsableItem
    {
        [SerializeField] private GameObject trapPrefab;
        [SerializeField] private float distanceBehind;
        public void Use(GameObject owner)
        {
            if (trapPrefab == null || owner == null)
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

            Instantiate(trapPrefab, spawnPos, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
