using System.Collections;
using UnityEngine;

namespace PJW
{
    /// <summary>
    /// PlayerRaceData�� KartSpeed�� ���� �ð� ���� ����� �÷ȴٰ� �����ϴ�
    /// �ּ� ��� �ν��� ������.
    /// </summary>
    public class BoostItem : MonoBehaviour, IUsableItem
    {
        [SerializeField] private float speedMultiplier = 1.5f; // ���� ���
        [SerializeField] private float duration = 3f;          // ���� �ð�(��)

        public void Use(GameObject owner)
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            var raceData = owner.GetComponentInParent<PlayerRaceData>();
            if (raceData == null)
            {
                Debug.LogWarning("[BoostItem] PlayerRaceData�� ã�� ���߽��ϴ�.");
                Destroy(gameObject);
                return;
            }

            StartCoroutine(BoostRoutine(raceData));
            Destroy(gameObject); // �������� ��� ��� ����
        }

        private IEnumerator BoostRoutine(PlayerRaceData data)
        {
            float original = data.KartSpeed;
            float boosted = original * Mathf.Max(0f, speedMultiplier);

            data.SetKartSpeed(boosted);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                // �߰��� �ٸ� �ý����� �ٲ㵵 �ν��� �ð� ������ ����
                data.SetKartSpeed(boosted);
                yield return null;
            }

            data.SetKartSpeed(original); // ����
        }
    }
}
