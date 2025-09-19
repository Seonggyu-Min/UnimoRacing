using System.Collections;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using YTW;

namespace PJW
{
    [RequireComponent(typeof(PhotonView))]
    public class BombTrap : MonoBehaviourPun
    {
        [Header("���� �ð�")]
        [SerializeField] private float stopDuration;

        [Header("���� Ű")]
        [SerializeField] private string sfxHitKey = "Bang_SFX";
        [SerializeField] private string sfxBlockedKey = "ShieldBlock";
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
                PlaySfxLocal(sfxBlockedKey, transform.position);

                // �����ڿ��� �ǵ� �Һ� RPC
                targetPv.RPC(nameof(PlayerShield.RpcConsumeShield), targetPv.Owner);

                hasTriggered = true;
                PhotonNetwork.Destroy(gameObject);
                return; 
            }

            PlaySfxLocal(sfxHitKey, transform.position);

            hasTriggered = true;

            photonView.RPC(nameof(RpcApplyBombStopOnOwner), targetPv.Owner, stopDuration);
            PhotonNetwork.Destroy(gameObject);
        }

        private void PlaySfxLocal(string key, Vector3 at)
        {
                AudioManager.Instance.PlaySFX(key);
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
            if (racer != null) racer.SetKartSpeed(original);
        }
    }
}
