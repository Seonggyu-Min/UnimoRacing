using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PJW
{
    public class EggTrapZone : MonoBehaviour
    {
        [Header("동작 파라미터")]
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
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;

            var racer = other.GetComponentInParent<PlayerRaceData>();
            if (racer == null) return;

            triggered = true;

            if (zoneCol) zoneCol.enabled = false;

            HideVisuals();

            StartCoroutine(BoostThenPinStop(racer));
        }

        private void HideVisuals()
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r) r.enabled = false;
            }
        }

        private IEnumerator BoostThenPinStop(PlayerRaceData racer)
        {
            float originalSpeed = racer.KartSpeed;

            // 가속
            racer.SetKartSpeed(Mathf.Max(0.1f, originalSpeed * boostMultiplier));
            yield return new WaitForSeconds(boostTime);

            // 평속
            racer.SetKartSpeed(originalSpeed);
            yield return new WaitForSeconds(waitAfterBoost);

            // 정지
            Vector3 pinPos = racer.transform.position;
            Quaternion pinRot = racer.transform.rotation;

            var cart = racer.GetComponent<Cinemachine.CinemachineDollyCart>();
            if (cart != null) cart.enabled = false;

            var rb = racer.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            float t = 0f;
            while (t < stopDuration)
            {
                racer.transform.SetPositionAndRotation(pinPos, pinRot);
                yield return null;
                t += Time.unscaledDeltaTime;
            }

            // 복구
            if (cart != null) cart.enabled = true;
            racer.SetKartSpeed(originalSpeed);

            Destroy(gameObject);
        }
    }
}
