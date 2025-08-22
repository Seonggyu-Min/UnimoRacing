using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PJW
{
    [RequireComponent(typeof(Collider))]
    public class BananaTrapZone : MonoBehaviour
    {
        [Header("동작 파라미터")]
        [SerializeField] private float boostMultiplier = 2f; 
        [SerializeField] private float boostTime = 1f;       
        [SerializeField] private float waitAfterBoost = 1f;  
        [SerializeField] private float stopDuration = 1.5f;  

        private bool triggered;
        private Collider zoneCol;

        private void Awake()
        {
            zoneCol = GetComponent<Collider>();
            zoneCol.isTrigger = true; // 트리거 강제

            var rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Debug.Log($"[Trap/Awake] {name} hasCol={zoneCol != null}, hasRB={rb != null}");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;

            var cart = other.GetComponentInParent<CinemachineDollyCart>();
            if (!cart) { Debug.LogWarning("[Trap] no DollyCart on hitter"); return; }

            triggered = true;

            if (zoneCol) zoneCol.enabled = false;

            StartCoroutine(BoostThenPinStop(cart));
        }

        private IEnumerator BoostThenPinStop(CinemachineDollyCart cart)
        {
            float originalSpeed = cart.m_Speed;
            Debug.Log($"[Trap/Start] origSpeed={originalSpeed}");

            // 가속
            cart.m_Speed = Mathf.Max(0.1f, originalSpeed * boostMultiplier);
            Debug.Log($"[Trap/Boost] speed={cart.m_Speed}");
            yield return new WaitForSeconds(boostTime);

            // 평속
            cart.m_Speed = originalSpeed;
            Debug.Log($"[Trap/Hold] speed={cart.m_Speed}");
            yield return new WaitForSeconds(waitAfterBoost);

            //  정지
            Vector3 pinPos = cart.transform.position;
            Quaternion pinRot = cart.transform.rotation;

            cart.enabled = false;

            var rb = cart.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            float t = 0f;
            while (t < stopDuration)
            {
                cart.transform.SetPositionAndRotation(pinPos, pinRot); 
                yield return null;
                t += Time.unscaledDeltaTime; 
            }

            // 복구
            cart.enabled = true;
            cart.m_Speed = originalSpeed;
            Debug.Log("[Trap/End] restored");

            Destroy(gameObject);
        }
    }
}
