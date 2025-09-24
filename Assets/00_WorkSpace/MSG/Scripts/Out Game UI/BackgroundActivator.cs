using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class BackgroundActivator : MonoBehaviour
    {
        [SerializeField] private GameObject _background;

        private void OnEnable()
        {
            if (_background != null && !_background.activeInHierarchy)
            {
                _background.SetActive(true);
            }
        }

        private void OnDisable()
        {
            if (_background != null && _background.activeInHierarchy)
            {
                _background.SetActive(false);
            }
        }
    }
}
