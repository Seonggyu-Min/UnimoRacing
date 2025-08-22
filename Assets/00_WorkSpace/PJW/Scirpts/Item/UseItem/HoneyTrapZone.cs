using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class HoneyTrapZone : MonoBehaviour
    {
        [SerializeField] private float slowMultiplier;
        [SerializeField] private float duration;        

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            var cart = other.GetComponentInParent<CinemachineDollyCart>();
            if (cart == null) return;

            StartCoroutine(TempSlow(cart));
        }

        private IEnumerator TempSlow(CinemachineDollyCart cart)
        {
            float original = cart.m_Speed;
            cart.m_Speed = original * slowMultiplier;
            yield return new WaitForSeconds(duration);
            cart.m_Speed = original;
        }
    }
}
