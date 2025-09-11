using EditorAttributes;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class MissionToDBUpdater : MonoBehaviour
    {
        [SerializeField] private MissionPatchSO _missionSO;

        // _missionSO를 기반으로 Firebase DB에 업데이트
        [Button("Update DB", 50f)]
        public void OnClickUpdate()
        {
            if (_missionSO == null)
            {
                Debug.LogError("[MissionToDBUpdater] MissionSO가 null입니다");
                return;
            }

            // DB에 보낼 트리 구성
            var payload = BuildPayload(_missionSO);

            // 업데이트
            DatabaseManager.Instance.SetOnMain(
                DBRoutes.MissionsRoot,
                payload,
                () => Debug.Log("[MissionToDBUpdater] 업로드 완료"),
                err => Debug.LogError($"[MissionToDBUpdater] 업로드 실패: {err}")
            );
        }

        private Dictionary<string, object> BuildPayload(MissionPatchSO so)
        {
            var root = new Dictionary<string, object>();
            root[DatabaseKeys.updatedAt] = ServerValue.Timestamp;

            var dailies = new Dictionary<string, object>();
            foreach (var e in so.Dailies)
            {
                Dictionary<string, object> row = new()
                {
                    [DatabaseKeys.title] = e.Title,
                    [DatabaseKeys.missionType] = e.MissionType.ToString(),
                    [DatabaseKeys.description] = e.Description,
                    [DatabaseKeys.moneyType] = e.MoneyType.ToString(),
                    [DatabaseKeys.rewardQuantity] = e.RewardQuantity,
                    [DatabaseKeys.targetCount] = e.TargetCount,
                    [DatabaseKeys.missionVerb] = e.MissionVerb.ToString(),
                    [DatabaseKeys.missionObject] = e.MissionObject.ToString(),
                    [DatabaseKeys.partyCondition] = e.PartyCondition.ToString(),
                };

                //if (!string.IsNullOrEmpty(e.SubKey)) row[DatabaseKeys.subtype] = e.SubKey;
                dailies[e.Index.ToString()] = row;
            }
            root[DatabaseKeys.daily] = dailies;

            var achievements = new Dictionary<string, object>();
            foreach (var e in so.Achievements)
            {
                Dictionary<string, object> row = new()
                {
                    [DatabaseKeys.title] = e.Title,
                    [DatabaseKeys.missionType] = e.MissionType.ToString(),
                    [DatabaseKeys.description] = e.Description,
                    [DatabaseKeys.moneyType] = e.MoneyType.ToString(),
                    [DatabaseKeys.rewardQuantity] = e.RewardQuantity,
                    [DatabaseKeys.targetCount] = e.TargetCount,
                    [DatabaseKeys.missionVerb] = e.MissionVerb.ToString(),
                    [DatabaseKeys.missionObject] = e.MissionObject.ToString(),
                    [DatabaseKeys.partyCondition] = e.PartyCondition.ToString(),
                };

                // if (!string.IsNullOrEmpty(e.SubKey)) row[DatabaseKeys.subtype] = e.SubKey;
                achievements[e.Index.ToString()] = row;
            }
            root[DatabaseKeys.achievement] = achievements;

            return root;
        }
    }
}
