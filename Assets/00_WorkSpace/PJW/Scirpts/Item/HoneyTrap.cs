using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PJW
{
    public class HoneyTrap : MonoBehaviour
    {
        [Header("���� ����")]
        [SerializeField] private GameObject trapPrefab;     
        [SerializeField] private float distanceAhead = 5f;  
        [Header("UI ��ư")]
        [SerializeField] private Button trapButton;        

        private void Start()
        {
            if (trapButton != null)
                trapButton.onClick.AddListener(PlaceTrap);
        }

        private void PlaceTrap()
        {
            if (trapPrefab == null) return;

            Vector3 spawnPos = transform.position + transform.forward * distanceAhead;
            Instantiate(trapPrefab, spawnPos, Quaternion.identity);
        }
    }
}
