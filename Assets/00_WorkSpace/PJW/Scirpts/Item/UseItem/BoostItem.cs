using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; 

namespace PJW
{
    public class BoostItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private float speedMultiplier;
        [SerializeField] private float duration;

        private static readonly Dictionary<int, Running> running = new Dictionary<int, Running>();

        private class Running
        {
            public float baseSpeed;
            public Coroutine routine;
        }

        public void Use(GameObject owner)
        {
            if (!owner) { Destroy(gameObject); return; }

            var data = owner.GetComponentInParent<PlayerRaceData>();
            if (!data) { Destroy(gameObject); return; }

            // ��� �ĺ� Ű
            int key = GetOwnerKey(owner, data);

            // 1) ���� ȿ���� ���� ������ "��� ��� + ���󺹱�"
            if (running.TryGetValue(key, out var cur) && cur?.routine != null)
            {
                data.StopCoroutine(cur.routine);                 // ���� �ڷ�ƾ �ߴ�
                data.SetKartSpeed(cur.baseSpeed);                // ���� �ӵ��� ����
                running.Remove(key);
            }

            float mul = Mathf.Max(1f, speedMultiplier);
            float baseSpeed = data.KartSpeed;                  
            float boosted = baseSpeed * mul;                   
            data.SetKartSpeed(boosted);

            var slot = new Running { baseSpeed = baseSpeed };
            slot.routine = data.StartCoroutine(BoostRoutine(data, key, slot, duration));
            running[key] = slot;

            Destroy(gameObject);
        }

        private static IEnumerator BoostRoutine(PlayerRaceData data, int key, Running slot, float dur)
        {
            float end = Time.unscaledTime + Mathf.Max(0f, dur);
            while (data && Time.unscaledTime < end)
                yield return null;

            if (data) data.SetKartSpeed(slot.baseSpeed); 
            running.Remove(key);
        }

        private static int GetOwnerKey(GameObject owner, PlayerRaceData data)
        {
            var pv = owner.GetComponentInParent<PhotonView>() ?? data.GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null) return pv.OwnerActorNr;
            return data.GetInstanceID(); 
        }
    }
}
