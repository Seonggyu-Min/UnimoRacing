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
        [SerializeField] private Image _incompletionProgressBar;    // 미완료 상태일 때의 게이지 바 (붉은색)
        [SerializeField] private Image _completionProgress;         // 완료 상태일 때의 게이지 바 (초록색)
        [SerializeField] private Image _goldIcon;                   // 보상 재화 종류 중 골드의 아이콘
        [SerializeField] private Image _blueHoneyGemIcon;           // 보상 재화 종류 중 블루허니잼의 아이콘



        public void Init()
        {

        }


        public void OnClickClaimReward()
        {

        }
    }
}
