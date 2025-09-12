// PJW.RocketItem.cs
using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class RocketItem : MonoBehaviour, IUsableItem
    {
        [Header("Resources Ű(���). ��: \"RocketProjectile\" �Ǵ� \"Projectiles/RocketProjectile\"")]
        [SerializeField] private string rocketResourceKey = "RocketProjectile";

        [Header("���� �ð�(��)")]
        [SerializeField] private float stunDuration = 1.5f;

        [Header("�߻� ��ġ ������ (���� ����)")]
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;

        [Header("Ÿ�� Ž�� �ִ� �Ÿ�")]
        [SerializeField] private float maxSearchDistance = 9999f;

        public void Use(GameObject owner)
        {
            // ����/������ üũ
            var ownerPv = owner ? owner.GetComponentInParent<PhotonView>() : null;
            if (ownerPv == null || !ownerPv.IsMine) { Destroy(gameObject); return; }

            // Ÿ�� ã��(��븸)
            var targetPv = FindNearestOpponent(owner.transform.position, ownerPv.ViewID, maxSearchDistance);
            if (targetPv == null) { Destroy(gameObject); return; }

            // Resources/�� üũ
            if (Resources.Load<GameObject>(rocketResourceKey) == null || !PhotonNetwork.InRoom)
            {
                Destroy(gameObject);
                return;
            }

            // ���� ��ġ/ȸ��
            Vector3 pos = owner.transform.TransformPoint(spawnOffset);
            Vector3 fwd = (targetPv.transform.position - pos).normalized;
            Quaternion rot = fwd.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(fwd, Vector3.up) : owner.transform.rotation;

            // �ν��Ͻ� ������: [0]=target ViewID, [1]=stunDuration
            object[] data = new object[] { targetPv.ViewID, stunDuration };
            PhotonNetwork.Instantiate(rocketResourceKey, pos, rot, 0, data);

            // ������ �Һ�
            Destroy(gameObject);
        }

        private PhotonView FindNearestOpponent(Vector3 from, int myViewId, float maxDist)
        {
            PhotonView nearest = null;
            float bestSqr = maxDist * maxDist;

            foreach (var pv in FindObjectsOfType<PhotonView>())
            {
                if (pv == null) continue;
                if (pv.ViewID == myViewId) continue;   // �ڱ� �ڽ� ����
                if (pv.Owner == null) continue;
                if (pv.IsMine) continue;               // �� ���� ������Ʈ ����(��븸)

                var cart = pv.GetComponentInChildren<Cinemachine.CinemachineDollyCart>(true);
                if (cart == null) continue;

                float sqr = (pv.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    nearest = pv;
                }
            }
            return nearest;
        }
    }
}
