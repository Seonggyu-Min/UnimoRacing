using UnityEngine;
using Cinemachine;
using Photon.Pun;

namespace PJW
{
    [DisallowMultipleComponent]
    public class ForceLaneSwitchZone : MonoBehaviour
    {
        public enum SwitchMode
        {
            Left,       
            Right,      
            Opposite,
        }

        [Header("스위치 방식")]
        [SerializeField] private SwitchMode switchMode = SwitchMode.Opposite;

        [Header("트리거 동작 옵션")]
        [SerializeField] private bool onlyAffectLocalOwner = true;  
        [SerializeField] private bool consumeOnTrigger = false;     
        [SerializeField] private float reuseDelay = 0f;             

        private bool isCoolingDown;

        private void OnTriggerEnter(Collider other)
        {
            if (isCoolingDown) return;

            var cart = other.GetComponentInParent<CinemachineDollyCart>();
            if (cart == null) return;

            var switcher = other.GetComponentInParent<DollyLaneSwitcher>();
            if (switcher == null) return;

            var pv = other.GetComponentInParent<PhotonView>();
            if (onlyAffectLocalOwner && pv != null && !pv.IsMine) return;

            switch (switchMode)
            {
                case SwitchMode.Left:
            switcher.ForceSwitchLeft();
            break;
                case SwitchMode.Right:
                switcher.ForceSwitchRight();
                break;
            case SwitchMode.Opposite:
                switcher.ForceSwitchOpposite();
                break;
            }

            if (consumeOnTrigger)
            {
                gameObject.SetActive(false);
            }
            else if (reuseDelay > 0f)
            {
                StartCoroutine(CooldownRoutine());
            }
        }

        private System.Collections.IEnumerator CooldownRoutine()
        {
            isCoolingDown = true;
            yield return new WaitForSeconds(reuseDelay);
            isCoolingDown = false;
        }
    }
}
