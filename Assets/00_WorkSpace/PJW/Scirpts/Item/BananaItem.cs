using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using System.Collections;

namespace PJW
{
    public class BananaItem : MonoBehaviour
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
}
