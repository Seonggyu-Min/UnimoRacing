using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    /// <summary>
    /// 경기 종료 및 미션 클리어 이후 재화를 얻기 위해 사용하는 컴포넌트 입니다.
    /// 재화 획득 시 미션에도 반영해야 되기 때문에 획득은 이 곳에서 일괄적으로 처리해야됩니다.
    /// </summary>
    public class RewardManager : Singleton<RewardManager>
    {
        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;

        private void Awake()
        {
            SingletonInit();
        }

        public void AddMoney(MoneyType moneyType, int quantity)
        {
            switch (moneyType)
            {
                case MoneyType.Gold:
                    DatabaseManager.Instance.IncrementToLongOnMainWithTransaction(DBRoutes.Gold(CurrentUid),
                        quantity,
                        suc =>
                        { 
                            Debug.Log($"골드 {quantity}을 얻음. 현재 골드: {suc}");
                            MissionService.Instance.Report(MissionVerb.Obtain, MissionObject.Gold, null, quantity); 
                        },
                        err => Debug.LogWarning($"골드 쓰기 오류: {err}"));

                    // TODO: 미션의 92013 ~ 92016 골드 누적 증가 처리 필요한데, Gold가 Money1이 아닐 수 있음. 추후 처리 필요
                    // 또한 현재 구조는 index에 강하게 묶여 있어서 index가 변경되면 현재 진행 상황이 뒤죽박죽 될 듯
                    // 그래서 애초에 고유한 key가 필요할 것 같음. ex) achv.gold.50000
                    // 그리고 아예 자동화처리 하기 위해서는 Mission_Type 이외에 타입을 훨씬 상세하게 정하는 필드가 필요할 듯
                    // 그 후 MissionService에서 다시 재조립 후 해당 타입이 변경이 되었다는 메서드를 만들고 호출받을 필요가 있을 듯
                    // ex) NotifyProgress(Type type(타입은 골드 획득, 완주, 아이템 획득 등), int delta) 이렇게 해서 패치로 업적이 바뀌더라도 호출부의 코드 변경이 없도록 해야 좋긴 할 듯
                    // 근데 여기서 미션이 삭제된다면 그냥 DB에서 삭제 하는 것이 아닌, 모든 플레이어를 순회하면서 해당 데이터를 아예 삭제하거나 Disabled = true 등으로 바꿔둬야 될 듯
                    //MissionService.Instance.IncrementAchievementProgress(92013, quantity);
                    //MissionService.Instance.IncrementAchievementProgress(92014, quantity);
                    //MissionService.Instance.IncrementAchievementProgress(92015, quantity);
                    //MissionService.Instance.IncrementAchievementProgress(92016, quantity);

                    
                    break;

                case MoneyType.BlueHoneyGem:
                    DatabaseManager.Instance.IncrementToLongOnMainWithTransaction(DBRoutes.BlueHoneyGem(CurrentUid),
                        quantity,
                        suc => 
                        { 
                            Debug.Log($"블루허니잼 {quantity}을 얻음. 현재 블루허니잼: {suc}");
                            MissionService.Instance.Report(MissionVerb.Obtain, MissionObject.BluyHoneyGem, null, quantity);
                        },
                        err => Debug.LogWarning($"블루허니잼 쓰기 오류: {err}"));
                    break;

                //case MoneyType.Money3:
                //    DatabaseManager.Instance.IncrementToLongOnMainWithTransaction(DBRoutes.Money3(CurrentUid),
                //        quantity,
                //        suc => Debug.Log($"돈3 {quantity}을 얻음. 현재 돈3: {suc}"),
                //        err => Debug.LogWarning($"돈3 쓰기 오류: {err}"));
                //    break;
            }
        }
    }
}
