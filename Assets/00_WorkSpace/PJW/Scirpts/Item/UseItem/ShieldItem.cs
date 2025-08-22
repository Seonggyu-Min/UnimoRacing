using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PJW
{
    public class ShieldItem : MonoBehaviour
    {
        [SerializeField] private float defaultDuration;

        private bool isInvulnerable;
        private float remainTime;

        public void Activate(float duration = -1f)
        {
            float d = duration > 0f ? duration : defaultDuration;
            if (d <= 0f) return;
            isInvulnerable = true;
            remainTime = d;
            Debug.Log($"[Shield] Activated for {d:0.##}s");
        }

        public bool TryBlock(string sourceTag = null)
        {
            if (isInvulnerable)
            {
                return true;
            }
            return false;
        }

        public bool IsInvulnerable()
        {
            return isInvulnerable;
        }

        private void Update()
        {
            if (!isInvulnerable) return;

            remainTime -= Time.deltaTime;
            if (remainTime <= 0f)
            {
                isInvulnerable = false;
                Debug.Log("[Shield] Expired");
            }
        }
    }
}
