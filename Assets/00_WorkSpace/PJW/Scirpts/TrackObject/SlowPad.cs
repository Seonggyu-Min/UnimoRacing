using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace PJW
{
    [DisallowMultipleComponent]
    public class SlowPad : MonoBehaviour
    {
        [Header("슬로우 설정")]
        [SerializeField] private float slowMultiplier = 0.5f;     
        [SerializeField] private float slowDuration = 2f;         
        [SerializeField] private bool canRetrigger = true;        
        [SerializeField] private float retriggerCooldown = 0.1f;  

        private class SlowState
        {
            public float originalSpeed;
            public float endTime;
            public Coroutine routine;
        }

        private readonly Dictionary<CinemachineDollyCart, SlowState> states = new();
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
            if (cart == null || slowMultiplier <= 0f || slowDuration <= 0f) return;

            if (!states.TryGetValue(cart, out var state))
            {
                state = new SlowState
                {
                    originalSpeed = cart.m_Speed,
                    endTime = Time.time + slowDuration
                };

                cart.m_Speed = state.originalSpeed * slowMultiplier;
                state.routine = StartCoroutine(RunSlow(cart, state));
                states[cart] = state;
            }
            else
            {
                state.endTime = Mathf.Max(state.endTime, Time.time + slowDuration);
            }

            lastTriggerTime = Time.time;
        }

        private IEnumerator RunSlow(CinemachineDollyCart cart, SlowState state)
        {
            while (Time.time < state.endTime)
                yield return null;

            cart.m_Speed = state.originalSpeed;
            states.Remove(cart);
        }

        private void OnDisable() => RestoreAll();
        private void OnDestroy() => RestoreAll();

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
