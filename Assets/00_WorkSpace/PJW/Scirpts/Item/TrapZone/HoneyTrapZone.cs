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

        private bool triggered;
        private Collider col;
        private Renderer[] rends;

        private void Awake()
        {
            col = GetComponent<Collider>();
            rends = GetComponentsInChildren<Renderer>(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;

            var shield = other.GetComponentInParent<PlayerShield>();
            if (shield != null && shield.SuccessShield())
            {
                triggered = true;
                if (col) col.enabled = false;
                if (rends != null) foreach (var r in rends) if (r) r.enabled = false;
                Destroy(gameObject);
                return;
            }

            var cart = other.GetComponentInParent<CinemachineDollyCart>();
            if (cart == null) return;

            triggered = true;

            // 비주얼만 끄고 오브젝트는 남겨 코루틴 유지
            if (col) col.enabled = false;
            if (rends != null) foreach (var r in rends) if (r) r.enabled = false;

            StartCoroutine(ApplySlowThenDestroy(cart));
        }

        private IEnumerator ApplySlowThenDestroy(CinemachineDollyCart cart)
        {
            if (cart == null || slowMultiplier <= 0f) { Destroy(gameObject); yield break; }

            cart.m_Speed *= slowMultiplier;

            yield return new WaitForSeconds(duration);

            if (slowMultiplier != 0f)
                cart.m_Speed /= slowMultiplier;

            Destroy(gameObject); 
        }
    }
}
