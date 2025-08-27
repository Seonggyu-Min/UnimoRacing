using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PJW
{
    public class BananaTrapZone : MonoBehaviour
    {
        [Header("���� �Ķ����")]
        [SerializeField] private float boostMultiplier = 2f; 
        [SerializeField] private float boostTime = 1f;       
        [SerializeField] private float waitAfterBoost = 1f;  
        [SerializeField] private float stopDuration = 1.5f;  

        private bool triggered;
        private Collider zoneCol;
        private Renderer[] renderers;

        private void Awake()
        {
            zoneCol = GetComponent<Collider>();
            var rb = GetComponent<Rigidbody>();
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;

            var cart = other.GetComponentInParent<CinemachineDollyCart>();
            if (cart == null) return;

            triggered = true;

            if (zoneCol) zoneCol.enabled = false;

            HideVisuals();

            StartCoroutine(BoostThenPinStop(cart));
        }

        private void HideVisuals()
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r) r.enabled = false;
            }
        }

        private IEnumerator BoostThenPinStop(CinemachineDollyCart cart)
        {
            float originalSpeed = cart.m_Speed;

            // ����
            cart.m_Speed = Mathf.Max(0.1f, originalSpeed * boostMultiplier);
            yield return new WaitForSeconds(boostTime);

            // ���
            cart.m_Speed = originalSpeed;
            yield return new WaitForSeconds(waitAfterBoost);

            //  ����
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

            // ����
            cart.enabled = true;
            cart.m_Speed = originalSpeed;

            Destroy(gameObject);
        }
    }
}
