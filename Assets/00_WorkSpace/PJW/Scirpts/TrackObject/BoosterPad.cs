using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class BoosterPad : MonoBehaviour
    {
        [Header("부스터 설정")]
        [SerializeField] private float boostAmount;   
        [SerializeField] private float duration;      
        [SerializeField] private string requiredTag = "Player";

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {

            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            var pv = other.GetComponentInParent<PhotonView>();
            if (pv == null)
            {
                return;
            }
            if (!pv.IsMine)
            {
                return;
            }

            var data = other.GetComponentInParent<PlayerRaceData>();
            if (data == null)
            {
                return;
            }

            StartCoroutine(BoostRoutine(data));
        }

        private IEnumerator BoostRoutine(PlayerRaceData data)
        {
            float baseSpeed = data.KartSpeed;
            float boosted = baseSpeed + boostAmount;

            data.SetKartSpeed(boosted);

            yield return new WaitForSeconds(duration);

            data.SetKartSpeed(baseSpeed);
        }
    }
}
