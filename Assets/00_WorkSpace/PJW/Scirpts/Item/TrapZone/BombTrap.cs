using Cinemachine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class BombTrap : MonoBehaviour
    {
        [SerializeField] private float stopDuration;


        private void OnTriggerEnter(Collider other)
        {
            var racer = other.GetComponentInParent<PlayerRaceData>();
            if (racer == null) return;

            racer.StartCoroutine(StopRacerTemporarily(racer, stopDuration));

            Destroy(gameObject);
        }

        private IEnumerator StopRacerTemporarily(PlayerRaceData racer, float duration)
        {
            if (racer == null) yield break;

            float originalSpeed = racer.KartSpeed;
            racer.SetKartSpeed(0f);

            yield return new WaitForSeconds(duration);

            if (racer != null)
                racer.SetKartSpeed(originalSpeed);
        }
    }
}
