using Cinemachine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class BombTrap : MonoBehaviour
    {
        [SerializeField] private float stopDuration;


        private void OnTriggerEnter(Collider other)
        {
            var cart = other.GetComponentInParent<CinemachineDollyCart>();
            if (cart == null) return;

            cart.StartCoroutine(StopCartTemporarily(cart, stopDuration));

            Destroy(gameObject);
        }

        private IEnumerator StopCartTemporarily(CinemachineDollyCart cart, float duration)
        {
            if (cart == null) yield break;

            float originalSpeed = cart.m_Speed;
            cart.m_Speed = 0f;

            yield return new WaitForSeconds(duration);

            if (cart != null)
                cart.m_Speed = originalSpeed;
        }
    }
}
