using Cinemachine;
using Photon.Pun;
using UnityEngine;
using YTW;

namespace PJW
{
    public class EggTrap : MonoBehaviour, IUsableItem
    {
        [Header("Resources")]
        [SerializeField] private string trapResourceKey = "EggTrapZone";

        [Header("È¿°úÀ½")]
        [SerializeField] private string sfxUseKey = "Egg_Use_SFX";

        [SerializeField] private float distanceBehind = 0.02f;

        public void Use(GameObject owner)
        {
            if (string.IsNullOrEmpty(trapResourceKey) || owner == null)
            {
                Destroy(gameObject);
                return;
            }

            var ownerPv = owner.GetComponentInParent<PhotonView>();
            if (ownerPv == null || !ownerPv.IsMine)
            {
                Destroy(gameObject);
                return;
            }

            var cart = owner.GetComponentInParent<CinemachineDollyCart>();
            var path = cart ? cart.m_Path : null;
            if (cart == null || path == null)
            {
                Destroy(gameObject);
                return;
            }

            float currentT = cart.m_Position;
            float behindT = currentT - distanceBehind;

            if (behindT > 1f && cart.m_Path.Looped)
                behindT -= 1f;
            else
                behindT = Mathf.Min(behindT, 1f);

            Vector3 spawnPos = cart.m_Path.EvaluatePositionAtUnit(
                behindT, CinemachinePathBase.PositionUnits.Normalized);

            if (Resources.Load<GameObject>(trapResourceKey) == null || !PhotonNetwork.InRoom)
            {
                Destroy(gameObject);
                return;
            }

            PhotonNetwork.Instantiate(trapResourceKey, spawnPos, Quaternion.identity);

            PlaySfxLocal(sfxUseKey);

            Destroy(gameObject);
        }

        private void PlaySfxLocal(string key)
        {
                AudioManager.Instance.PlaySFX(key); 
        }
    }
}
