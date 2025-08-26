using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class BuyButtonBehaviour : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Button _button;

        public void SetupUnimo(UnimoSO unimo)
        {

        }

        public void SetupKart(KartSO kart)
        {

        }

        public void SetOwnedVisual(bool owned)
        {
            _icon.color = owned ? Color.white : Color.gray; // 가지지 않은 것은 회색 처리
            _button.interactable = !owned; // 가진 것은 클릭 금지
        }

        public void OnClickBuyButton()
        {
            // 먼저 돈이 있는지 확인

            // 획득 처리
        }
    }
}
