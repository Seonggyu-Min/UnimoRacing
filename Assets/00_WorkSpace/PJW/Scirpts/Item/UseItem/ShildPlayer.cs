using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    [DisallowMultipleComponent]
    public class PlayerShield : MonoBehaviour
    {
        [Header("상태")]
        [SerializeField] private int charges = 0;        
        [SerializeField] private float timeLeft = 0f;    

        [Header("표시(선택)")]
        [SerializeField] private GameObject shieldIndicator; 

        public bool HasShield => charges > 0 && (timeLeft <= 0f || timeLeft > 0f);

        private void Update()
        {
            if (charges <= 0) return;
            if (timeLeft > 0f)
            {
                timeLeft -= Time.deltaTime;
                if (timeLeft <= 0f)
                {
                    charges = 0;
                    RefreshIndicator();
                }
            }
        }

        public void AddShield(int addCharges = 1, float durationSeconds = 0f)
        {
            charges += Mathf.Max(1, addCharges);
            timeLeft = Mathf.Max(0f, durationSeconds);
            RefreshIndicator();
        }

        public bool TryConsume()
        {
            if (charges <= 0) return false;

            charges--;
            if (charges <= 0)
            {
                timeLeft = 0f;
            }
            RefreshIndicator();
            return true;
        }

        private void RefreshIndicator()
        {
            if (shieldIndicator != null)
                shieldIndicator.SetActive(charges > 0 && (timeLeft <= 0f || timeLeft > 0f));
        }

        public static bool TryConsume(GameObject target)
        {
            if (target == null) return false;
            var ps = target.GetComponent<PlayerShield>() ?? target.GetComponentInParent<PlayerShield>();
            return ps != null && ps.TryConsume();
        }

        public static void Give(GameObject target, int count = 1, float durationSeconds = 0f)
        {
            if (target == null) return;
            var ps = target.GetComponent<PlayerShield>() ?? target.GetComponentInParent<PlayerShield>();
            if (ps == null) ps = target.AddComponent<PlayerShield>();
            ps.AddShield(count, durationSeconds);
        }
    }
}
