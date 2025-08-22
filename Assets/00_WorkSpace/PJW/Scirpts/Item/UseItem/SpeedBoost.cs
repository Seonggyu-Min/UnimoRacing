using UnityEngine;
using Cinemachine;

namespace PJW
{
    public class BoostItemExample : MonoBehaviour, IUsableItem
    {
        [SerializeField] private float boostSpeed = 10f;
        [SerializeField] private float duration = 2f;

        public void Use(GameObject owner)
        {
            var cart = owner.GetComponentInChildren<CinemachineDollyCart>();
            if (cart == null) { Destroy(gameObject); return; }

            owner.GetComponent<MonoBehaviour>().StartCoroutine(BoostRoutine(cart));
        }

        private System.Collections.IEnumerator BoostRoutine(CinemachineDollyCart cart)
        {
            float original = cart.m_Speed;
            cart.m_Speed = boostSpeed;
            yield return new WaitForSeconds(duration);
            cart.m_Speed = original;
            Destroy(gameObject);
        }
    }
}
