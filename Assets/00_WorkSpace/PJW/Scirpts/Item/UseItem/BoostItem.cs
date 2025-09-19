using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YTW;

namespace PJW
{
    public class BoostItem : MonoBehaviour, IUsableItem
    {
        [Header("�ν�Ʈ �Ķ����")]
        [SerializeField] private float speedMultiplier = 2f;
        [SerializeField] private float duration = 2f;

        [Header("���� Ű")]
        [SerializeField] private string sfxUse = "Boost_Start_SFX";   // ��� ���
        [SerializeField] private string sfxActive = "Boost_SFX";      // ��� 0.5s ��
        [SerializeField] private string sfxEnd = "Boost_End_SFX";     // ���� ��

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

            int key = GetOwnerKey(owner, data);

            // ���� ȿ���� ���� ������ ��� ��� + ���󺹱�
            if (running.TryGetValue(key, out var cur) && cur?.routine != null)
            {
                data.StopCoroutine(cur.routine);
                data.SetKartSpeed(cur.baseSpeed);
                running.Remove(key);
            }

            // ��� ��� SFX
            if (!string.IsNullOrEmpty(sfxUse))
                AudioManager.Instance.PlaySFX(sfxUse);

            float mul = Mathf.Max(1f, speedMultiplier);
            float baseSpeed = data.KartSpeed;
            float boosted = baseSpeed * mul;
            data.SetKartSpeed(boosted);

            // ��� 0.5�� �� SFX (Active)
            if (!string.IsNullOrEmpty(sfxActive))
                data.StartCoroutine(PlayDelayedSfx(sfxActive, 1f));

            var slot = new Running { baseSpeed = baseSpeed };
            slot.routine = data.StartCoroutine(BoostRoutine(data, key, slot, duration, sfxEnd));
            running[key] = slot;

            Destroy(gameObject);
        }

        private static IEnumerator BoostRoutine(PlayerRaceData data, int key, Running slot, float dur, string endSfx)
        {
            float end = Time.unscaledTime + Mathf.Max(0f, dur);
            while (data && Time.unscaledTime < end)
                yield return null;

            if (data)
            {
                data.SetKartSpeed(slot.baseSpeed);

                // ���� �� SFX
                if (!string.IsNullOrEmpty(endSfx))
                    AudioManager.Instance.PlaySFX(endSfx);
            }

            running.Remove(key);
        }

        private static IEnumerator PlayDelayedSfx(string key, float delay)
        {
            yield return new WaitForSeconds(delay);
            AudioManager.Instance.PlaySFX(key);
        }

        private static int GetOwnerKey(GameObject owner, PlayerRaceData data)
        {
            var pv = owner.GetComponentInParent<PhotonView>() ?? data.GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null) return pv.OwnerActorNr;
            return data.GetInstanceID();
        }
    }
}
