using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class BombTrap : MonoBehaviour
    {
        [SerializeField] private float stopDuration = 2f;

        private void OnTriggerEnter(Collider other)
        {
            CinemachineDollyCart cart = other.GetComponentInParent<CinemachineDollyCart>();
            if (cart != null)
            {
                StartCoroutine(StopCartTemporarily(cart));
            }
        }

        private IEnumerator StopCartTemporarily(CinemachineDollyCart cart)
        {
            float originalSpeed = cart.m_Speed;
            cart.m_Speed = 0f;

            yield return new WaitForSeconds(stopDuration);

            cart.m_Speed = originalSpeed;
        }
    }
}
