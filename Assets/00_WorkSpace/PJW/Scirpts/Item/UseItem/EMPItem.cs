using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;


namespace PJW
{
    public class EMPItem : MonoBehaviour
    {
        [SerializeField] private Button useButton;
        [SerializeField] private float stopDuration;

        private CinemachineDollyCart cart;
        private Rigidbody rb;
        private float originalSpeed;
        private bool rbWasKinematic;

        private void Awake()
        {
            cart = GetComponent<CinemachineDollyCart>();
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            if (useButton != null) useButton.onClick.AddListener(Use);
        }

        public void Use()
        {
            if (cart != null) StartCoroutine(StopAndRestore());
        }

        private IEnumerator StopAndRestore()
        {
            originalSpeed = cart.m_Speed;
            cart.m_Speed = 0f;
            cart.enabled = false;

            yield return new WaitForSeconds(stopDuration);

            if (rb != null) rb.isKinematic = rbWasKinematic;
            cart.enabled = true;
            cart.m_Speed = originalSpeed;
        }
    }
}
