using System.Collections;
using UnityEngine;

namespace PJW
{
    public class BoostItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private float speedMultiplier; 
        [SerializeField] private float duration;        

        public void Use(GameObject owner)
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            var raceData = owner.GetComponentInParent<PlayerRaceData>();
            if (raceData == null) { Destroy(gameObject); return; }

            raceData.StartCoroutine(BoostRoutine(raceData));

            Destroy(gameObject);
        }

        private IEnumerator BoostRoutine(PlayerRaceData data)
        {
            float original = data.KartSpeed;
            float boosted = original * Mathf.Max(0f, speedMultiplier);

            data.SetKartSpeed(boosted);
            yield return new WaitForSecondsRealtime(duration);

            data.SetKartSpeed(original);
        }
    }
}
