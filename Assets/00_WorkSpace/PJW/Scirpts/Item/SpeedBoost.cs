using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

namespace PJW
{
    public class SpeedBoost : MonoBehaviour
    {
        [Header("부스트 설정")]
        [SerializeField] private float boostAmount;   
        [SerializeField] private float boostDuration;  

        [Header("UI 버튼")]
        [SerializeField] private Button boostButton; 

        private CinemachineDollyCart dollyCart;
        private bool isBoosting = false;
        private float baseSpeed;

        private void Awake()
        {
            dollyCart = GetComponent<CinemachineDollyCart>();
        }

        private void Start()
        {
            if (boostButton != null)
            {
                boostButton.onClick.AddListener(OnClickBoost);
            }
        }

        private void OnClickBoost()
        {
            if (isBoosting) return;

            baseSpeed = dollyCart.m_Speed;
            StartCoroutine(BoostRoutine());
        }

        private IEnumerator BoostRoutine()
        {
            isBoosting = true;
            if (boostButton != null) boostButton.interactable = false;

            dollyCart.m_Speed = baseSpeed + boostAmount;
            yield return new WaitForSeconds(boostDuration);

            dollyCart.m_Speed = baseSpeed;
            isBoosting = false;
            if (boostButton != null) boostButton.interactable = true;
        }
    }
}
