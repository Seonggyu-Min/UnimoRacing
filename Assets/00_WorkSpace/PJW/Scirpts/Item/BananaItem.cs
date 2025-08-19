using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using System.Collections;

namespace PJW
{
    public class BananaTrap : MonoBehaviour
    {
        [SerializeField] private Button spawnButton;
        [SerializeField] private GameObject trapPrefab;
        [SerializeField] private float spawnDistance;

        private void Start()
        {
            if (spawnButton != null) spawnButton.onClick.AddListener(SpawnTrap);
        }

        private void SpawnTrap()
        {
            Vector3 pos = transform.position + transform.forward * spawnDistance;
            Instantiate(trapPrefab, pos, Quaternion.identity);
        }
    }

    public class BananaTrapZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var cart = other.GetComponentInParent<CinemachineDollyCart>();
            var rb = other.attachedRigidbody ?? other.GetComponentInParent<Rigidbody>();
            if (cart == null || rb == null) return;

            StartCoroutine(Slip(cart, rb, other.transform));
            Destroy(gameObject);
        }

        private IEnumerator Slip(CinemachineDollyCart cart, Rigidbody rb, Transform hit)
        {
            float speed = cart.m_Speed;

            cart.enabled = false;
            rb.isKinematic = false;

            rb.velocity = hit.forward * speed;
            float sign = Random.value < 0.5f ? -1f : 1f;
            rb.AddForce(hit.right * 6f * sign, ForceMode.Impulse);
            rb.AddTorque(Vector3.up * 10f * sign, ForceMode.Impulse);

            yield return new WaitForSeconds(1f);

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            cart.enabled = true;
            cart.m_Speed = speed;
        }
    }
}
