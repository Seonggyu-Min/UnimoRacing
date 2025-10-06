using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class CameraPrioritySwitcher : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera _mainCam;
        [SerializeField] private CinemachineVirtualCamera _fallbackCam;

        [SerializeField] private int _fallbackOn = 20;
        [SerializeField] private int _fallbackOff = 0;

        [SerializeField] private LayerMask _trackLayers;

        [SerializeField] private float _minTime = 1f;

        private Coroutine _changeCO;
        private WaitForSeconds wait;


        private void Awake()
        {
            wait = new WaitForSeconds(_minTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & _trackLayers) == 0) return;

            Debug.Log("[CameraPrioritySwitcher] OnTriggerEnter");
            StartCO();
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & _trackLayers) == 0) return;

            Debug.Log("[CameraPrioritySwitcher] OnTriggerExit");
        }

        private void StartCO()
        {
            if (_changeCO != null)
            {
                StopCoroutine(_changeCO);
                _changeCO = null;
            }
            _changeCO = StartCoroutine(ChangeForWhileRoutine());
        }

        private void StopCO()
        {
            if (_changeCO != null)
            {
                StopCoroutine(_changeCO);
                _changeCO = null;
            }
        }

        private IEnumerator ChangeForWhileRoutine()
        {
            _fallbackCam.m_Priority = _fallbackOn;
            if (wait != null)
            {
                //yield return wait;
                yield return new WaitForSeconds(_minTime);
            }
            _fallbackCam.m_Priority = _fallbackOff;
        }
    }
}
