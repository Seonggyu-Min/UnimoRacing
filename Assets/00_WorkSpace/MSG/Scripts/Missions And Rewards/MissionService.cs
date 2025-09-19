using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public struct MatchKey : IEquatable<MatchKey>
    {
        public MissionVerb Verb;
        public MissionObject Obj;
        public PartyCondition Party;
        public string Subtype;

        public bool Equals(MatchKey other) =>
            Verb == other.Verb && Obj == other.Obj && Party == other.Party &&
            string.Equals(Subtype, other.Subtype);

        public override int GetHashCode() =>
            HashCode.Combine(Verb, Obj, Party, Subtype ?? "");
    }

    /// <summary>
    /// MissionEntry에 포함되지 않는 개인별 진행도, 미션 달성 여부, 미션 보상 수령 여부 등을 포함하기 위한 클래스입니다.
    /// </summary>
    public class MissionWrapper
    {
        public MissionGroup MissionGroup;       // 데일리인지, 도전과제인지
        public MissionEntry MissionEntry;
        public int Progress;                    // 진행도
        public bool Cleared;                    // 미션 달성 여부
        public bool Claimed;                    // 미션 보상 수령 여부
    }


    public class MissionService : Singleton<MissionService>
    {
        #region Fields and Properties

        [SerializeField] private RewardPanelBehaviour _rewardPanel; // 이거 띄우기 위해서, TODO: 이거 전체가 버튼으로써 OnClick에 SetActive false 지정되면 될 듯

        private readonly Dictionary<int, MissionEntry> _dailyEntries = new();
        private readonly Dictionary<int, MissionEntry> _achievementEntries = new();
        private readonly Dictionary<MatchKey, List<int>> _dailyIndex = new();
        private readonly Dictionary<MatchKey, List<int>> _achvIndex = new();

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
                    ParseMissionEntries(snap);
                    Debug.Log("[MissionService] 미션 로딩 성공");

                    EnsureDailyOfToday(); // 일단 여기서 호출함
                },
                err => Debug.LogWarning($"[MissionService] 미션 로딩 실패: {err}")
            );
        }

        // 미션 파싱 및 캐싱
        private void ParseMissionEntries(DataSnapshot root)
        {
            _dailyEntries.Clear();
            _achievementEntries.Clear();

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
                        MoneyType = ToEnum(child.Child(DatabaseKeys.moneyType).Value?.ToString(), MoneyType.Gold),
                        MissionVerb = ToEnum(child.Child(DatabaseKeys.missionVerb).Value?.ToString(), MissionVerb.Obtain),
                        MissionObject = ToEnum(child.Child(DatabaseKeys.missionObject).Value?.ToString(), MissionObject.Item),
                        PartyCondition = ToEnum(child.Child(DatabaseKeys.partyCondition).Value?.ToString(), PartyCondition.Any),
                        // SubKey 추가 필요 시 삽입
                    };
                    _dailyEntries[index] = entry;
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
                        MoneyType = ToEnum(child.Child(DatabaseKeys.moneyType).Value?.ToString(), MoneyType.Gold),
                        MissionVerb = ToEnum(child.Child(DatabaseKeys.missionVerb).Value?.ToString(), MissionVerb.Obtain),
                        MissionObject = ToEnum(child.Child(DatabaseKeys.missionObject).Value?.ToString(), MissionObject.Item),
                        PartyCondition = ToEnum(child.Child(DatabaseKeys.partyCondition).Value?.ToString(), PartyCondition.Any),
                        // SubKey 추가 필요 시 삽입
                    };
                    _achievementEntries[index] = entry;
                }
            }

            BuildIndexes();
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

        public void Report(MissionVerb verb, MissionObject obj, bool? isParty, int quantity = 1 /*, string subtype = null*/) // SubType은 일단 안씀
        {
            var partyKey = isParty == true ? PartyCondition.True : PartyCondition.False;

            var key = new MatchKey { Verb = verb, Obj = obj, Party = partyKey, Subtype = null };

            // 매칭된 미션들 수집
            var daily = _dailyIndex.TryGetValue(key, out var d) ? d : null;
            var achv = _achvIndex.TryGetValue(key, out var a) ? a : null;

            quantity = Mathf.Max(1, quantity);

            if (daily != null && daily.Count > 0) IncrementDailyMissions(daily, quantity);
            if (achv != null && achv.Count > 0) IncrementAchvMissions(achv, quantity);
        }

        /// <summary>
        /// 데일리 보상을 수령합니다
        /// </summary>
        /// <param name="index">보상을 수령할 미션의 index</param>
        public void ClaimDaily(int index, Action OnSucces = null, Action<string> OnError = null)
        {
            if (!_dailyEntries.TryGetValue(index, out MissionEntry entry))
            {
                Debug.LogWarning($"데일리 미션이 _dailyEntries에 없습니다: {index}");
                return;
            }

            DatabaseManager.Instance.RunTransactionOnMain(
                DBRoutes.UserDailyMissionRoot(CurrentUid),
                mutable =>
                {
                    // cleared == true && claimed == false일 때만 수령
                    bool cleared = ToBool(mutable.Child(DatabaseKeys.cleared).Child(index.ToString()).Value ?? false);
                    bool claimed = ToBool(mutable.Child(DatabaseKeys.claimed).Child(index.ToString()).Value ?? false);
                    if (!cleared || claimed)
                    {
                        return TransactionResult.Abort();
                    }

                    // 수령 마크
                    mutable.Child(DatabaseKeys.claimed).Child(index.ToString()).Value = true;
                    return TransactionResult.Success(mutable);
                },
                _ =>
                {
                    // 지급
                    RewardManager.Instance.AddMoney(entry.MoneyType, entry.RewardQuantity);
                    // 수령 UI 표기
                    _rewardPanel.Init(entry.MoneyType, entry.RewardQuantity);
                    UIManager.Instance.Show("Reward Panel");

                    OnSucces?.Invoke();
                    Debug.Log("[MissionService] 데일리 미션 보상 수령 완료");
                },
                err =>
                {
                    OnError?.Invoke(err);
                    Debug.Log($"[MissionService] 데일리 미션 보상 수령 실패: {err}");
                }
            );
        }

        /// <summary>
        /// Achievement을 보상 수령합니다
        /// </summary>
        /// <param name="index">보상을 수령할 미션의 index</param>
        public void ClaimAchievement(int index, Action OnSucces = null, Action<string> OnError = null)
        {
            if (!_achievementEntries.TryGetValue(index, out MissionEntry entry))
            {
                OnError?.Invoke($"achievement 미션이 _achievementEntries 없습니다: {index}");
                return;
            }

            Dictionary<string, object> updates = new()
            {
                { DBRoutes.UserAchievementMissionClaimed(CurrentUid, index), true },
            };


            DatabaseManager.Instance.UpdateOnMain(
                updates,
                () =>
                {
                    // 지급
                    RewardManager.Instance.AddMoney(entry.MoneyType, entry.RewardQuantity);
                    // 수령 UI 표기
                    _rewardPanel.Init(entry.MoneyType, entry.RewardQuantity);
                    UIManager.Instance.Show("Reward Panel");

                    OnSucces?.Invoke();
                    Debug.Log("[MissionService] achievement 미션 보상 수령 완료");
                },
                err =>
                {
                    OnError?.Invoke(err);
                    Debug.Log($"[MissionService] achievement 미션 보상 수령 실패: {err}");
                });



            DatabaseManager.Instance.RunTransactionOnMain(
                DBRoutes.UserAchievementMissionRoot(CurrentUid),
                mutable =>
                {
                    // cleared == true && claimed == false일 때만 수령
                    bool cleared = ToBool(mutable.Child(DatabaseKeys.cleared).Child(index.ToString()).Value ?? false);
                    bool claimed = ToBool(mutable.Child(DatabaseKeys.claimed).Child(index.ToString()).Value ?? false);
                    if (!cleared || claimed)
                        return TransactionResult.Abort();

                    // 수령 마크
                    mutable.Child(DatabaseKeys.claimed).Child(index.ToString()).Value = true;
                    return TransactionResult.Success(mutable);
                },
                _ => 
                {
                    // 지급
                    RewardManager.Instance.AddMoney(entry.MoneyType, entry.RewardQuantity);
                    // 수령 UI 표기
                    _rewardPanel.Init(entry.MoneyType, entry.RewardQuantity);
                    UIManager.Instance.Show("Reward Panel");

                    OnSucces?.Invoke();
                    Debug.Log("[MissionService] achievement 미션 보상 수령 완료");
                } ,
                err => 
                {
                    OnError?.Invoke(err);
                    Debug.Log($"[MissionService] achievement 미션 보상 수령 실패: {err}");
                } 
            );
        }

        /// <summary>
        /// 유저의 미션 현황을 확인합니다.
        /// </summary>
        public void LoadUserMissions(Action<List<MissionWrapper>> onSuccess, Action<string> onError = null)
        {
            DatabaseManager.Instance.GetOnMain(
                DBRoutes.UserMissionsRoot(CurrentUid),
                snap =>
                {
                    var dailySnap = snap?.Child(DatabaseKeys.daily);
                    var achvSnap = snap?.Child(DatabaseKeys.achievement);

                    var wrapper = BuildMissionWrapper(dailySnap, achvSnap);
                    onSuccess?.Invoke(wrapper);
                },
                err => onError?.Invoke(err)
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

        private bool ToBool(object v)
        {
            if (v == null) return false;
            bool.TryParse(v.ToString(), out bool b);
            return b;
        }

        private string GetServerDateKey()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        // 특정 키 조합에 해당하는 미션 빠른 조회용 Dict 생성
        private void BuildIndexes()
        {
            _dailyIndex.Clear();
            _achvIndex.Clear();

            foreach (var e in _dailyEntries.Values)
            {
                if (e.PartyCondition == PartyCondition.Any)
                {
                    // Any라면 양쪽 Dict에 캐싱해서 런타임 조회 빠르게 함
                    Add(_dailyIndex, e, PartyCondition.True);
                    Add(_dailyIndex, e, PartyCondition.False);
                }
                else
                {
                    Add(_dailyIndex, e, e.PartyCondition);
                }
            }

            foreach (var e in _achievementEntries.Values)
            {
                if (e.PartyCondition == PartyCondition.Any)
                {
                    Add(_achvIndex, e, PartyCondition.True);
                    Add(_achvIndex, e, PartyCondition.False);
                }
                else
                {
                    Add(_achvIndex, e, e.PartyCondition);
                }
            }

            Debug.Log("BuildIndexes 호출 완료");
        }

        private void Add(Dictionary<MatchKey, List<int>> dict, MissionEntry e, PartyCondition partyKey)
        {
            var key = new MatchKey { Verb = e.MissionVerb, Obj = e.MissionObject, Party = partyKey, Subtype = null }; // SubType은 일단 안써서 null로 넣어둠
            if (!dict.TryGetValue(key, out var list))
            {
                dict[key] = list = new List<int>();
            }
            list.Add(e.Index);
        }

        private void IncrementDailyMissions(List<int> ids, int delta)
        {
            DatabaseManager.Instance.RunTransactionOnMain(
                DBRoutes.UserDailyMissionRoot(CurrentUid),
                mutable =>
                {
                    // 날짜가 다르면 리셋
                    string today = GetServerDateKey();
                    string dateKey = mutable.Child(DatabaseKeys.dateKey).Value?.ToString();
                    if (dateKey != today)
                    {
                        mutable.Child(DatabaseKeys.progress).Value = null;
                        mutable.Child(DatabaseKeys.cleared).Value = null;
                        mutable.Child(DatabaseKeys.claimed).Value = null;
                        mutable.Child(DatabaseKeys.dateKey).Value = today;
                    }

                    // 일치하는 미션의 진행도 증가
                    foreach (var id in ids)
                    {
                        // 현재까지의 진행도 확인
                        var pNode = mutable.Child(DatabaseKeys.progress).Child(id.ToString());
                        int prev = 0;
                        if (pNode.Value != null)
                        {
                            int.TryParse(pNode.Value.ToString(), out prev);
                        }

                        // 현재까지의 진행도 += 추가된 진행도
                        int next = Math.Max(0, prev + delta);
                        mutable.Child(DatabaseKeys.progress).Child(id.ToString()).Value = next;

                        // 목표에 도달했으면 완료 처리
                        if (_dailyEntries.TryGetValue(id, out var entry) && next >= entry.TargetCount)
                        {
                            mutable.Child(DatabaseKeys.cleared).Child(id.ToString()).Value = true;
                        }
                    }
                    return TransactionResult.Success(mutable);
                },
                _ => { Debug.Log("[MissionService] 데일리 미션 업데이트 완료"); },
                err => Debug.LogWarning(err)
            );
        }

        private void IncrementAchvMissions(List<int> ids, int delta)
        {
            DatabaseManager.Instance.RunTransactionOnMain(
                DBRoutes.UserAchievementMissionRoot(CurrentUid),
                mutable =>
                {
                    // 일치하는 미션의 진행도 증가
                    foreach (var id in ids)
                    {
                        // 현재까지의 진행도 확인
                        var pNode = mutable.Child(DatabaseKeys.progress).Child(id.ToString());
                        int prev = 0;
                        if (pNode.Value != null)
                        {
                            int.TryParse(pNode.Value.ToString(), out prev);
                        }

                        // 현재까지의 진행도 += 추가된 진행도
                        int next = Math.Max(0, prev + delta);
                        mutable.Child(DatabaseKeys.progress).Child(id.ToString()).Value = next;

                        // 목표에 도달했으면 완료 처리
                        if (_achievementEntries.TryGetValue(id, out var entry) && next >= entry.TargetCount)
                        {
                            mutable.Child(DatabaseKeys.cleared).Child(id.ToString()).Value = true;
                        }
                    }
                    return TransactionResult.Success(mutable);
                },
                _ => { Debug.Log("[MissionService] Achievement 미션 업데이트 완료"); },
                err => Debug.LogWarning(err)
            );
        }

        private List<MissionWrapper> BuildMissionWrapper(DataSnapshot dailySnap, DataSnapshot achvSnap)
        {
            var list = new List<MissionWrapper>();
            list.AddRange(BuildMissionWrapperForGroup(MissionGroup.Daily, _dailyEntries, dailySnap));
            list.AddRange(BuildMissionWrapperForGroup(MissionGroup.Achievement, _achievementEntries, achvSnap));
            return list;
        }

        private List<MissionWrapper> BuildMissionWrapperForGroup(MissionGroup group, Dictionary<int, MissionEntry> entries, DataSnapshot snap)
        {
            List<MissionWrapper> wrapper = new();
            if (entries == null) return wrapper;

            foreach (var kv in entries)
            {
                int index = kv.Key;
                var entry = kv.Value;

                int progress = ToInt(snap?.Child(DatabaseKeys.progress)?.Child(index.ToString())?.Value);
                bool cleared = ToBool(snap?.Child(DatabaseKeys.cleared)?.Child(index.ToString())?.Value);
                bool claimed = ToBool(snap?.Child(DatabaseKeys.claimed)?.Child(index.ToString())?.Value);

                wrapper.Add(new MissionWrapper
                {
                    MissionGroup = group,
                    MissionEntry = entry,
                    Progress = progress,
                    Cleared = cleared,
                    Claimed = claimed
                });
            }
            return wrapper;
        }

        #endregion
    }
}
