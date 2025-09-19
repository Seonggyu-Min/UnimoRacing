using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class RewardEffectRotator : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 50f;


        private void Update()
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }
}
