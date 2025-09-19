using Photon.Pun;
using System.Collections;
using System.Linq;
using UnityEngine;
using YTW;

namespace PJW
{
    [RequireComponent(typeof(PhotonView))]
    public class BombTrap : MonoBehaviourPun
    {
        [SerializeField] private float stopDuration = 1.5f;

        [Header("���� Ű")]
        [SerializeField] private string sfxBlockedKey = "Shield_Block_SFX";   // ����� ������ ��
        [SerializeField] private string sfxHitKey = "Bang";    // Ʈ�� �ߵ����� ��

        private bool hasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (!PhotonNetwork.IsMasterClient || hasTriggered)
                return;

            var targetPv = other.GetComponentInParent<PhotonView>();
            if (targetPv == null || targetPv.Owner == null)
                return;

            var shield = targetPv.GetComponent<PlayerShield>() ??
                         targetPv.GetComponentInChildren<PlayerShield>(true);

            if (shield != null && shield.IsShieldActive)
            {
                targetPv.RPC(nameof(PlayerShield.RpcConsumeShield), targetPv.Owner);
                AudioManager.Instance.PlaySFX(sfxBlockedKey); // ���� ���� ����
                hasTriggered = true;
                PhotonNetwork.Destroy(gameObject);
                return;
            }

            AudioManager.Instance.PlaySFX(sfxHitKey); // ��Ʈ ����
            hasTriggered = true;

            photonView.RPC(nameof(RpcApplyBombStopOnOwner), targetPv.Owner, stopDuration);
            PhotonNetwork.Destroy(gameObject);
        }

        [PunRPC]
        private void RpcApplyBombStopOnOwner(float duration)
        {
            var myRacer = FindObjectsOfType<PlayerRaceData>(true)
                .FirstOrDefault(r =>
                {
                    var pv = r.GetComponentInParent<PhotonView>() ?? r.GetComponent<PhotonView>();
                    return pv != null && pv.IsMine;
                });

            if (myRacer != null)
                myRacer.StartCoroutine(CoStop(myRacer, duration));
        }

        private IEnumerator CoStop(PlayerRaceData racer, float duration)
        {
            float original = racer.KartSpeed;
            racer.SetKartSpeed(0f);
            yield return new WaitForSeconds(duration);
            if (racer != null)
                racer.SetKartSpeed(original);
        }
    }
}
