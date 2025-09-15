using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class MissionUIBehaviour : MonoBehaviour
    {
        [SerializeField] private TMP_Text _missionNameText;         // 미션 이름
        [SerializeField] private TMP_Text _countText;               // 진행 상황 ( 예시 - 350/500 )
        [SerializeField] private Image _completionProgress;         // 완료 상태일 때의 게이지 바 (초록색)
        [SerializeField] private Image _incompletionProgressBar;    // 미완료 상태일 때의 게이지 바 (붉은색)
        [SerializeField] private Image _goldIcon;                   // 보상 재화 종류 중 골드의 아이콘
        [SerializeField] private Image _blueHoneyGemIcon;           // 보상 재화 종류 중 블루허니잼의 아이콘

        private MissionWrapper _missionWrapper;


        public void Init(MissionWrapper missionWrapper)
        {
            _missionWrapper = missionWrapper;

            if (_missionWrapper == null)
            {
                Debug.LogWarning("[MissionUIBehaviour] _missionWrapper가 null입니다.");
                return;
            }

            MakeText();
            MakeProgressBar();
            MakeMoneyIcon();
        }


        public void OnClickClaimReward()
        {
            if (_missionWrapper == null)
            {
                Debug.LogWarning("[MissionUIBehaviour] _missionWrapper가 null입니다.");
                return;
            }
            if (!_missionWrapper.Cleared || _missionWrapper.Claimed)
            {
                Debug.Log("[MissionUIBehaviour] 이미 수령했거나 완료하지 않은 미션입니다.");
                return;
            }

            if (_missionWrapper.MissionGroup == MissionGroup.Daily)
            {
                MissionService.Instance.ClaimDaily(_missionWrapper.MissionEntry.Index);
            }
            else
            {
                MissionService.Instance.ClaimAchievement(_missionWrapper.MissionEntry.Index);
            }
        }


        private void MakeText()
        {
            _missionNameText.text = _missionWrapper.MissionEntry.Title;
            _countText.text = $"{_missionWrapper.Progress}/{_missionWrapper.MissionEntry.TargetCount}";
        }

        private void MakeProgressBar()
        {
            int target = Mathf.Max(1, _missionWrapper.MissionEntry.TargetCount);
            float ratio = Mathf.Clamp01((float)_missionWrapper.Progress / target);

            if (_missionWrapper.Cleared)
            {
                _completionProgress.gameObject.SetActive(true);
                _incompletionProgressBar.gameObject.SetActive(false);
                // 완료되었으면 FillAmount 조절할 필요없이 그대로 두면 됨
            }
            else
            {
                _completionProgress.gameObject.SetActive(false);
                _incompletionProgressBar.gameObject.SetActive(true);

                // 미완료니까 FillAmount 비율 조절
                _incompletionProgressBar.fillAmount = ratio;
            }
        }

        private void MakeMoneyIcon()
        {
            if (_missionWrapper.MissionEntry.MoneyType == MoneyType.Gold)
            {
                _goldIcon.gameObject.SetActive(true);
                _blueHoneyGemIcon.gameObject.SetActive(false);
            }
            else
            {
                _goldIcon.gameObject.SetActive(false);
                _blueHoneyGemIcon.gameObject.SetActive(true);
            }
        }
    }
}
