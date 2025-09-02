using System.Linq;
using Photon.Pun;
using UnityEngine;
using Cinemachine;

namespace PJW
{
    public class RocketItem : MonoBehaviour, IUsableItem
    {
        [Header("���� ������")]
        [SerializeField] private string rocketPrefabName = "RocketProjectile";
        [Header("���� �ð�(��)")]
        [SerializeField] private float stunDuration;
        [Header("�߻� ��ġ ������")]
        [SerializeField] private Vector3 spawnOffset = new Vector3();
        [Header("Ÿ�� Ž�� �Ÿ�")]
        [SerializeField] private float maxSearchDistance = 9999f;

        public void Use(GameObject owner)
        {
            var ownerPv = owner.GetComponentInParent<PhotonView>();
            if (ownerPv == null || !ownerPv.IsMine) return;

            var targetPv = FindNearestOpponent(owner.transform.position, ownerPv.ViewID, maxSearchDistance);
            if (targetPv == null) return;

            Vector3 pos = owner.transform.TransformPoint(spawnOffset);
            Quaternion rot = Quaternion.LookRotation((targetPv.transform.position - pos).normalized, Vector3.up);

            object[] data = new object[] { targetPv.ViewID, stunDuration };
            PhotonNetwork.Instantiate(rocketPrefabName, pos, rot, 0, data);

            Destroy(gameObject); 
        }

        private PhotonView FindNearestOpponent(Vector3 from, int myViewId, float maxDist)
        {
            PhotonView nearest = null;
            float bestSqr = maxDist * maxDist;

            foreach (var pv in FindObjectsOfType<PhotonView>())
            {
                if (pv.ViewID == myViewId) continue; 
                if (pv.IsMine) continue;             
                if (pv.Owner == null) continue;      

                var cart = pv.GetComponentInChildren<CinemachineDollyCart>(true);
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
