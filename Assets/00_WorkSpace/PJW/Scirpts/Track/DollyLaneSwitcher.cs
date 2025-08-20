using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PJW
{
    [RequireComponent(typeof(CinemachineDollyCart))]
    public class DollyLaneSwitcher : MonoBehaviour
    {
        [SerializeField] private CinemachinePathBase[] lanes;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        private CinemachineDollyCart cart;
        private int laneIndex;

        private void Awake()
        {
            cart = GetComponent<CinemachineDollyCart>();

            for (int i = 0; i < lanes.Length; i++)
            {
                if (lanes[i] == cart.m_Path) { laneIndex = i; break; }
            }

            if (leftButton != null) leftButton.onClick.AddListener(MoveLeft);
            if (rightButton != null) rightButton.onClick.AddListener(MoveRight);
        }

        public void MoveLeft() => SetLane(laneIndex - 1);
        public void MoveRight() => SetLane(laneIndex + 1);

        private void SetLane(int idx)
        {
            if (lanes == null || lanes.Length == 0) return;
            idx = Mathf.Clamp(idx, 0, lanes.Length - 1);
            if (idx == laneIndex) return;

            float t = (cart.m_PositionUnits == CinemachinePathBase.PositionUnits.Normalized)
                ? (float)cart.m_Position
                : 0f;

            cart.m_Path = lanes[idx];
            if (cart.m_PositionUnits == CinemachinePathBase.PositionUnits.Normalized)
                cart.m_Position = t;

            laneIndex = idx;
        }
    }
}
