using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PJW
{
    public class HoneyTrap : MonoBehaviour, IUsableItem
    {
        [SerializeField] private GameObject trapPrefab;
        [SerializeField] private float distanceAhead = 5f;

        public void Use(GameObject owner)
        {
            if (trapPrefab == null || owner == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 spawnPos = owner.transform.position + owner.transform.forward * distanceAhead;
            Instantiate(trapPrefab, spawnPos, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
