using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using System.Collections;

namespace PJW
{
    public class BananaTrap : MonoBehaviour, IUsableItem
    {
        [SerializeField] private GameObject trapPrefab;
        [SerializeField] private float spawnDistance = 2f;
        public void Use(GameObject owner)
        {
            if (trapPrefab == null || owner == null) return;

            Vector3 pos = owner.transform.position + owner.transform.forward * spawnDistance;
            Instantiate(trapPrefab, pos, Quaternion.identity);

            Destroy(gameObject);
        }
    }    
}
