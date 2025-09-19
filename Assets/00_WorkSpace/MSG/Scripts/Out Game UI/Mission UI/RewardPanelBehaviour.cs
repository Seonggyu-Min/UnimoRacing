using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class RewardPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private Image _goldIcon;
        [SerializeField] private Image _blueHoneyGemIcon;


        public void Init(MoneyType moneyType, int amount)
        {
            if (moneyType == MoneyType.Gold)
            {
                Debug.Log("MoneyType.Gold");
                _goldIcon.gameObject.SetActive(true);
                _blueHoneyGemIcon.gameObject.SetActive(false);
            }
            else if (moneyType == MoneyType.BlueHoneyGem)
            {
                Debug.Log("MoneyType.BlueHoneyGem");
                _goldIcon.gameObject.SetActive(false);
                _blueHoneyGemIcon.gameObject.SetActive(true);
            }

            _amountText.text = amount.ToString();
        }

        public void OnTouchToClose()
        {
            UIManager.Instance.Hide("Reward Panel");
        }
    }
}
