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
        [SerializeField] private MissionListLoader _missionListLoader;

        private MoneyType _moneyType;
        private Coroutine _waitCO;

        private void Start()
        {
            MissionService.Instance.RegisterRewardPanel(this);
        }

        public void Init(MoneyType moneyType, int amount)
        {
            if (moneyType == MoneyType.Gold)
            {
                _moneyType = moneyType;
                Debug.Log("MoneyType.Gold");

                if (_waitCO != null)
                {
                    StopCoroutine( _waitCO );
                    _waitCO = null;
                }
                _waitCO = StartCoroutine(WaitAndDisable());
            }
            else if (moneyType == MoneyType.BlueHoneyGem)
            {
                _moneyType = moneyType;
                Debug.Log("MoneyType.BlueHoneyGem");

                if (_waitCO != null)
                {
                    StopCoroutine(_waitCO);
                    _waitCO = null;
                }
                _waitCO = StartCoroutine(WaitAndDisable());
            }

            _amountText.text = amount.ToString();
        }

        public void OnTouchToClose()
        {
            _missionListLoader.RenewUI();
            UIManager.Instance.Hide("Reward Panel");
        }

        private IEnumerator WaitAndDisable()
        {
            yield return null;  // 애니메이터에서 골드랑 블루허니잼을 둘 다 만져서 활성화되는 것 같음
                                // 근본적으로는 Target이 특정 상황에서, 외부에서 실행되지 않게 하는 플래그가 필요함

            _goldIcon.gameObject.SetActive(_moneyType == MoneyType.Gold);
            _blueHoneyGemIcon.gameObject.SetActive(_moneyType == MoneyType.BlueHoneyGem);
        }
    }
}
