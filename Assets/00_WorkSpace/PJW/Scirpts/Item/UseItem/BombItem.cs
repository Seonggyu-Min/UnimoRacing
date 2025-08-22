using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    
    public class BombItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private float distanceAhead = 5f;

        public void Use(GameObject owner)
        {
            if (bombPrefab == null || owner == null) return;

            Vector3 spawnPos = owner.transform.position + owner.transform.forward * distanceAhead;
            Instantiate(bombPrefab, spawnPos, Quaternion.identity);
        }
    }
}
