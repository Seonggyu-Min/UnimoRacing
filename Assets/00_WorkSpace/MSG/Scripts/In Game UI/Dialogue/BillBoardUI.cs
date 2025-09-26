using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class BillBoardUI : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            if (_camera != null)
            {
                transform.LookAt(transform.position + _camera.transform.rotation * Vector3.forward,
                    _camera.transform.rotation * Vector3.up);
            }
        }
    }
}
