using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace PJW
{
    [DisallowMultipleComponent]
    public class BoosterPad : MonoBehaviour
    {
        [Header("부스터 설정")]
        [SerializeField] private float boostMultiplier = 2f;     
        [SerializeField] private float boostDuration = 2f;       
        [SerializeField] private bool canRetrigger = true;       
        [SerializeField] private float retriggerCooldown = 0.1f; 

        private class BoostState
        {
            public float originalSpeed;
            public float endTime;
            public Coroutine routine;
        }

        private readonly Dictionary<CinemachineDollyCart, BoostState> states = new();
        private float lastTriggerTime;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!canRetrigger && Time.time - lastTriggerTime < retriggerCooldown) return;

            var cart = other.GetComponentInParent<CinemachineDollyCart>();
            if (cart == null || boostMultiplier <= 0f || boostDuration <= 0f) return;

            if (!states.TryGetValue(cart, out var state))
            {
                state = new BoostState
                {
                    originalSpeed = cart.m_Speed,
                    endTime = Time.time + boostDuration
                };

                cart.m_Speed = state.originalSpeed * boostMultiplier;
                state.routine = StartCoroutine(RunBoost(cart, state));
                states[cart] = state;
            }
            else
            {
                state.endTime = Mathf.Max(state.endTime, Time.time + boostDuration);
            }

            lastTriggerTime = Time.time;
        }

        private IEnumerator RunBoost(CinemachineDollyCart cart, BoostState state)
        {
            while (Time.time < state.endTime)
                yield return null;

            cart.m_Speed = state.originalSpeed;

            states.Remove(cart);
        }

        private void OnDisable()
        {
            RestoreAll();
        }

        private void OnDestroy()
        {
            RestoreAll();
        }

        private void RestoreAll()
        {
            foreach (var kv in states)
            {
                var cart = kv.Key;
                var state = kv.Value;
                if (state.routine != null) StopCoroutine(state.routine);
                if (cart != null) cart.m_Speed = state.originalSpeed;
            }
            states.Clear();
        }
    }
}
