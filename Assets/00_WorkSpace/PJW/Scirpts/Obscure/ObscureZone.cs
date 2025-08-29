using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class ObscureZone : MonoBehaviour
    {
        [Header("���� �̹��� ������")]
        [SerializeField] private GameObject obscureImagePrefab;

        [Header("���� �ð�")]
        [SerializeField] private float duration = 2f;

        private void OnTriggerEnter(Collider other)
        {
            var cart = other.GetComponentInParent<CinemachineDollyCart>();
            if (cart == null) return;

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            if (obscureImagePrefab == null)
            {
                return;
            }

            GameObject imageInstance = Instantiate(obscureImagePrefab, canvas.transform);
            imageInstance.transform.SetAsLastSibling(); 
            Destroy(imageInstance, duration);
        }
    }
}
