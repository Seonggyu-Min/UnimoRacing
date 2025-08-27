using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using System.Collections;

namespace PJW
{
    public class BananaTrap : MonoBehaviour, IUsableItem
    {
        [SerializeField] private GameObject trapPrefab;
        [SerializeField] private float distanceBehind;
        public void Use(GameObject owner)
        {
            if (trapPrefab == null || owner == null) return;

            var cart = owner.GetComponentInParent<CinemachineDollyCart>();
            if (cart == null || cart.m_Path == null)
            {
                Destroy(gameObject);
                return;
            }

            float currentT = cart.m_Position;                 
            float behindT = currentT - distanceBehind;          

            if (behindT > 1f && cart.m_Path.Looped)
                behindT -= 1f;
            else
                behindT = Mathf.Min(behindT, 1f);

            Vector3 spawnPos = cart.m_Path.EvaluatePositionAtUnit(
                behindT, CinemachinePathBase.PositionUnits.Normalized);

            Instantiate(trapPrefab, spawnPos, Quaternion.identity);

            Destroy(gameObject);
        }
    }    
}
