using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


namespace MSG
{
    public class MissionService : Singleton<MissionService>
    {
        #region Fields and Properties

        private readonly Dictionary<int, MissionEntry> _dailyDefs = new();
        private readonly Dictionary<int, MissionEntry> _achievementDefs = new();

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;

        #endregion


        #region Unity Methods

        private void Awake()
        {
            SingletonInit();
        }

        private void Start()
        {
            LoadMissionsFromServer();
        }

        #endregion


        #region Init Methods

        // 미션 로드
        private void LoadMissionsFromServer()
        {
            DatabaseManager.Instance.GetOnMain(
                DBRoutes.MissionsRoot,
                snap =>
                {
                    ParseMissionDefs(snap);
                    Debug.Log("[MissionService] 미션 로딩 성공");
                },
                err => Debug.LogWarning($"[MissionService] 미션 로딩 실패: {err}")
            );
        }

        // 미션 파싱 및 캐싱
        private void ParseMissionDefs(DataSnapshot root)
        {
            _dailyDefs.Clear();
            _achievementDefs.Clear();

            // 데일리 미션
            var dailyNode = root.Child(DatabaseKeys.daily);
            if (dailyNode.Exists)
            {
                foreach (var child in dailyNode.Children)
                {
                    if (!int.TryParse(child.Key, out int index)) continue;

                    var entry = new MissionEntry
                    {
                        Index = index,
                        Title = child.Child(DatabaseKeys.title).Value?.ToString(),
                        Description = child.Child(DatabaseKeys.description).Value?.ToString(),
                        RewardQuantity = ToInt(child.Child(DatabaseKeys.rewardQuantity).Value),
                        TargetCount = ToInt(child.Child(DatabaseKeys.targetCount).Value),
                        MissionType = ToEnum(child.Child(DatabaseKeys.missionType).Value?.ToString(), MissionType.RaceFinish),
                        MoneyType = ToEnum(child.Child(DatabaseKeys.moneyType).Value?.ToString(), MoneyType.Money1),
                    };
                    _dailyDefs[index] = entry;
                }
            }

            // achievement
            var achvNode = root.Child(DatabaseKeys.achievement);
            if (achvNode.Exists)
            {
                foreach (var child in achvNode.Children)
                {
                    if (!int.TryParse(child.Key, out int index)) continue;

                    var entry = new MissionEntry
                    {
                        Index = index,
                        Title = child.Child(DatabaseKeys.title).Value?.ToString(),
                        Description = child.Child(DatabaseKeys.description).Value?.ToString(),
                        RewardQuantity = ToInt(child.Child(DatabaseKeys.rewardQuantity).Value),
                        TargetCount = ToInt(child.Child(DatabaseKeys.targetCount).Value),
                        MissionType = ToEnum(child.Child(DatabaseKeys.missionType).Value?.ToString(), MissionType.RaceFinish),
                        MoneyType = ToEnum(child.Child(DatabaseKeys.moneyType).Value?.ToString(), MoneyType.Money1),
                    };
                    _achievementDefs[index] = entry;
                }
            }
        }

        #endregion


        #region Public API Methods

        // TODO: 기본적으로는 Start에서 호출하되, 시간을 체크하고 24:00 시간이 지날 때마다 콜백해주는 곳이 필요할 듯
        /// <summary>
        /// 데일리 미션의 dateKey가 오늘과 다르면 progress/cleared/claimed를 초기화합니다.
        /// </summary>
        public void EnsureDailyOfToday()
        {
            DatabaseManager.Instance.RunTransactionOnMain(
                DBRoutes.UserDailyMissionRoot(CurrentUid),
                mutable =>
                {
                    string today = GetServerDateKey(); // 서버 기반으로 개선 여지 있음
                    string dateKey = mutable.Child(DatabaseKeys.dateKey).Value?.ToString();

                    if (dateKey != today)
                    {
                        mutable.Child(DatabaseKeys.progress).Value = null;
                        mutable.Child(DatabaseKeys.cleared).Value = null;
                        mutable.Child(DatabaseKeys.claimed).Value = null;
                        mutable.Child(DatabaseKeys.dateKey).Value = today;
                    }
                    return TransactionResult.Success(mutable);
                },
                _ => Debug.Log("[MissionService] 데일리 미션 초기화 완료"),
                err => Debug.Log($"[MissionService] 데일리 미션 초기화 실패: {err}")
            );
        }

        /// <summary>
        /// 데일리 진행도를 증가시킨 후 목표 달성 시 목표 달성 처리합니다.
        /// </summary>
        /// <param name="index">진행도를 증가시킬 미션의 index</param>
        /// <param name="delta">진행도를 증가시킬 수치</param>
        public void IncrementDailyProgress(int index, int delta)
        {
            if (!_dailyDefs.TryGetValue(index, out MissionEntry def))
            {
                Debug.LogWarning($"데일리 미션이 _dailyDefs에 없습니다: {index}");
                return;
            }

            DatabaseManager.Instance.RunTransactionOnMain(
                DBRoutes.UserDailyMissionRoot(CurrentUid),
                mutable =>
                {
                    // 날짜 보정
                    string today = GetServerDateKey();
                    string dateKey = mutable.Child(DatabaseKeys.dateKey).Value?.ToString();
                    if (dateKey != today) // 날짜가 다르면 reset
                    {
                        mutable.Child(DatabaseKeys.progress).Value = null;
                        mutable.Child(DatabaseKeys.cleared).Value = null;
                        mutable.Child(DatabaseKeys.claimed).Value = null;
                        mutable.Child(DatabaseKeys.dateKey).Value = today;
                    }

                    // progress/{index}에 delta 더하고 업데이트
                    var pNode = mutable.Child(DatabaseKeys.progress).Child(index.ToString());
                    int prev = 0;
                    if (pNode.Value != null)
                    {
                        int.TryParse(pNode.Value.ToString(), out prev);
                    }
                    int next = Math.Max(0, prev + delta);
                    mutable.Child(DatabaseKeys.progress).Child(index.ToString()).Value = next;

                    // 만약 목표 달성했으면 cleared = true
                    if (next >= def.TargetCount)
                    {
                        mutable.Child(DatabaseKeys.cleared).Child(index.ToString()).Value = true;
                    }

                    return TransactionResult.Success(mutable);
                },
                _ => Debug.Log("[MissionService] 데일리 미션 업데이트 완료"),
                err => Debug.Log($"[MissionService] 데일리 미션 업데이트 실패: {err}")
            );
        }

        /// <summary>
        /// achievement 진행도를 증가시킨 후 목표 달성 시 목표 달성 처리합니다.
        /// </summary>
        /// <param name="index">진행도를 증가시킬 미션의 index</param>
        /// <param name="delta">진행도를 증가시킬 수치</param>
        public void IncrementAchievementProgress(int index, int delta)
        {
            if (!_achievementDefs.TryGetValue(index, out MissionEntry def))
            {
                Debug.LogWarning($"achievement 미션이 _achievementDefs에 없습니다: {index}");
                return;
            }

            DatabaseManager.Instance.RunTransactionOnMain(
                DBRoutes.UserAchievementMissionRoot(CurrentUid),
                mutable =>
                {
                    // progress/{index}에 delta 더하고 업데이트
                    var pNode = mutable.Child(DatabaseKeys.progress).Child(index.ToString());
                    int prev = 0;
                    if (pNode.Value != null)
                    {
                        int.TryParse(pNode.Value.ToString(), out prev);
                    }
                    int next = Math.Max(0, prev + delta);
                    mutable.Child(DatabaseKeys.progress).Child(index.ToString()).Value = next;

                    // 만약 목표 달성했으면 cleared = true
                    if (next >= def.TargetCount)
                    {
                        mutable.Child(DatabaseKeys.cleared).Child(index.ToString()).Value = true;
                    }

                    return TransactionResult.Success(mutable);
                },
                _ => Debug.Log("[MissionService] achievement 미션 업데이트 완료"),
                err => Debug.Log($"[MissionService] achievement 미션 업데이트 실패: {err}")
            );
        }

        /// <summary>
        /// 데일리 보상을 수령합니다
        /// </summary>
        /// <param name="index">보상을 수령할 미션의 index</param>
        public void ClaimDaily(int index)
        {
            if (!_dailyDefs.TryGetValue(index, out MissionEntry def))
            {
                Debug.LogWarning($"데일리 미션이 _dailyDefs에 없습니다: {index}");
                return;
            }

            DatabaseManager.Instance.RunTransactionOnMain(
                DBRoutes.UserDailyMissionRoot(CurrentUid),
                mutable =>
                {
                    // cleared == true && claimed == false일 때만 수령
                    bool cleared = Convert.ToBoolean(mutable.Child(DatabaseKeys.cleared).Child(index.ToString()).Value ?? false);
                    bool claimed = Convert.ToBoolean(mutable.Child(DatabaseKeys.claimed).Child(index.ToString()).Value ?? false);
                    if (!cleared || claimed)
                    {
                        return TransactionResult.Abort();
                    }

                    // 지급
                    RewardManager.Instance.AddMoney(def.MoneyType, def.RewardQuantity);

                    // 수령 마크
                    mutable.Child(DatabaseKeys.claimed).Child(index.ToString()).Value = true;
                    return TransactionResult.Success(mutable);
                },
                _ => Debug.Log("[MissionService] 데일리 미션 보상 수령 완료"),
                err => Debug.Log($"[MissionService] 데일리 미션 보상 수령 실패: {err}")
            );
        }

        /// <summary>
        /// Achievement을 보상 수령합니다
        /// </summary>
        /// <param name="index">보상을 수령할 미션의 index</param>
        public void ClaimAchievement(int index)
        {
            if (!_achievementDefs.TryGetValue(index, out MissionEntry def))
            {
                Debug.LogWarning($"achievement 미션이 _achievementDefs 없습니다: {index}");
                return;
            }

            DatabaseManager.Instance.RunTransactionOnMain(
                DBRoutes.UserAchievementMissionRoot(CurrentUid),
                mutable =>
                {
                    // cleared == true && claimed == false일 때만 수령
                    bool cleared = Convert.ToBoolean(mutable.Child(DatabaseKeys.cleared).Child(index.ToString()).Value ?? false);
                    bool claimed = Convert.ToBoolean(mutable.Child(DatabaseKeys.claimed).Child(index.ToString()).Value ?? false);
                    if (!cleared || claimed)
                        return TransactionResult.Abort();

                    // 지급
                    RewardManager.Instance.AddMoney(def.MoneyType, def.RewardQuantity);

                    // 수령 마크
                    mutable.Child(DatabaseKeys.claimed).Child(index.ToString()).Value = true;
                    return TransactionResult.Success(mutable);
                },
                _ => Debug.Log("[MissionService] achievement 미션 보상 수령 완료"),
                err => Debug.Log($"[MissionService] achievement 미션 보상 수령 실패: {err}")
            );
        }

        #endregion


        #region Private Methods

        private int ToInt(object v)
        {
            if (v == null) return 0;
            int.TryParse(v.ToString(), out int r);
            return r;
        }

        private TEnum ToEnum<TEnum>(string s, TEnum fallback) where TEnum : struct
        {
            return Enum.TryParse<TEnum>(s, out var e) ? e : fallback;
        }

        private string GetServerDateKey()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        #endregion
    }
}
