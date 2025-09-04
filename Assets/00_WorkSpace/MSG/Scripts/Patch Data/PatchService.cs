using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class PatchService : Singleton<PatchService>
    {
        // TODO: 각 인덱스별 카트 속도를 전부 캐싱해두는 구조도 가능하긴 한데, 필요할지는 모르겠음
        #region Fields

        private SpeedRuleDTO _globalSpeed;
        private readonly Dictionary<int, SpeedRuleDTO> _speedOverrides = new();

        private CostRuleDTO _globalCost;
        private readonly Dictionary<int, CostRuleDTO> _costOverrides = new();

        private UnimoCostDTO _globalUnimoCost;
        private readonly Dictionary<int, UnimoCostDTO> _unimoOverrides = new();

        private bool _patchReady;

        #endregion


        #region Unity Methods

        private void Awake() => SingletonInit();

        private void Start() => LoadPatchFromServer(
            () => Debug.Log("[PatchService] 패치 초기화 완료"),
            err => Debug.LogError(err)
            );

        #endregion


        #region Public API Methods

        // DB 스냅샷에서 /patch 구조를 읽어 캐시 구축. 한 번은 읽어야 됨. 그리고 FirebaseDatabase가 준비 되었을 때 읽기 시도해야 됨.
        public void LoadPatchFromServer(Action onSuccess = null, Action<string> onError = null)
        {
            DatabaseManager.Instance.GetOnMain(
                 DBRoutes.PatchRoot(),
                 snap =>
                 {
                     if (snap == null || !snap.Exists)
                     {
                         _patchReady = false;
                         onError?.Invoke("[PatchService] /patch 노드가 없습니다");
                         return;
                     }

                     var gSpeed = snap.Child(DatabaseKeys.globals).Child(DatabaseKeys.speed);
                     var gKartCost = snap.Child(DatabaseKeys.globals).Child(DatabaseKeys.cost);
                     var gUnimoCost = snap.Child(DatabaseKeys.globals).Child(DatabaseKeys.unimoCost);
                     _globalSpeed = ParseSpeed(gSpeed);
                     _globalCost = ParseCost(gKartCost);
                     _globalUnimoCost = ParseUnimoCost(gUnimoCost);

                     _speedOverrides.Clear();
                     _costOverrides.Clear();
                     _unimoOverrides.Clear();

                     var karts = snap.Child(DatabaseKeys.karts);
                     if (karts.Exists)
                     {
                         foreach (var c in karts.Children)
                         {
                             if (!int.TryParse(c.Key, out int kartId)) continue;

                             if (c.HasChild(DatabaseKeys.speedOverride) &&
                                 Convert.ToBoolean(c.Child(DatabaseKeys.speedOverride).Value) &&
                                 c.HasChild(DatabaseKeys.speed))
                             {
                                 _speedOverrides[kartId] = ParseSpeed(c.Child(DatabaseKeys.speed));
                             }

                             if (c.HasChild(DatabaseKeys.costOverride) &&
                                 Convert.ToBoolean(c.Child(DatabaseKeys.costOverride).Value) &&
                                 c.HasChild(DatabaseKeys.cost))
                             {
                                 _costOverrides[kartId] = ParseCost(c.Child(DatabaseKeys.cost));
                             }
                         }
                     }

                     var unimos = snap.Child(DatabaseKeys.unimos);
                     if (unimos.Exists)
                     {
                         foreach (var u in unimos.Children)
                         {
                             if (!int.TryParse(u.Key, out int unimoId)) continue;
                             if (u.HasChild(DatabaseKeys.costOverride) && Convert.ToBoolean(u.Child(DatabaseKeys.costOverride).Value))
                             {
                                 _unimoOverrides[unimoId] = new()
                                 {
                                     moneyType = ReadString(u, DatabaseKeys.moneyType),
                                     cost = ReadInt(u, DatabaseKeys.cost)
                                 };
                             }
                         }
                     }

                     _patchReady = true;
                     onSuccess?.Invoke();
                 },
                 err => onError?.Invoke($"[PatchService] 패치 조회 실패: {err}")
             );
        }

        // 현재 자신 인벤토리의 카트 레벨을 한 번 읽어야되기 때문에 비동기로 처리해야됨
        /// <summary>
        /// 카트 인덱스와 해당 카트의 강화 수치를 계산하여 해당하는 카트의 속도를 반환해주는 메서드입니다.
        /// </summary>
        /// <param name="kartId">확인하고자 하는 카트의 인덱스를 넣습니다.</param>
        /// <param name="onSuccess">계산에 성공했을 때 받을 Action float을 전달합니다. float에는 속도가 대입됩니다.</param>
        /// <param name="onError">계산에 실패했을 때 받을 Action string을 전달합니다. string에는 에러 메시지가 대입됩니다. (Optional Parameter)</param>
        public void GetSpeedOfKart(int kartId, Action<float> onSuccess, Action<string> onError = null)
        {
            if (!_patchReady)
            {
                onError?.Invoke("[PatchService] 패치를 불러오지 못하여 속도를 반환할 수 없습니다");
                return;
            }
            var auth = FirebaseManager.Instance?.Auth?.CurrentUser;
            if (auth == null)
            {
                onError?.Invoke("[PatchService] 유저가 null입니다");
                return;
            }

            DatabaseManager.Instance.GetOnMain(
                DBRoutes.KartInventory(auth.UserId, kartId),
                snap =>
                {
                    // 레벨 파싱
                    int level = 0;
                    if (snap.Exists && snap.Value != null)
                    {
                        int.TryParse(snap.Value.ToString(), out level);
                    }

                    // 0이면 미보유
                    if (level <= 0)
                    {
                        onSuccess?.Invoke(0f);
                        Debug.LogWarning($"[PatchService] 미보유한 카트{kartId}를 조회하였습니다");
                        return;
                    }

                    // 오버라이드가 있는지 먼저 확인 후 없으면 글로벌 룰 적용
                    var rule = _globalSpeed;
                    if (_speedOverrides.TryGetValue(kartId, out var ovr)) rule = ovr;

                    // 최종 속도 계산
                    float speed = ComputeSpeed(rule, level);
                    onSuccess?.Invoke(speed);
                },
                err =>
                {
                    onError?.Invoke(err);
                }
            );
        }


        public void GetCostOfKart(int kartId, Action<int, MoneyType> onSuccess, Action<string> onError = null)
        {
            if (!_patchReady)
            {
                onError?.Invoke("[PatchService] 패치를 불러오지 못하여 가격을 반환할 수 없습니다");
                return;
            }
            var auth = FirebaseManager.Instance?.Auth?.CurrentUser;
            if (auth == null)
            {
                onError?.Invoke("[PatchService] 유저가 null입니다");
                return;
            }

            DatabaseManager.Instance.GetOnMain(
                    DBRoutes.KartInventory(auth.UserId, kartId),
                    snap =>
                    {
                        // 레벨 파싱
                        int level = 0;
                        if (snap != null && snap.Exists && snap.Value != null)
                        {
                            int.TryParse(snap.Value.ToString(), out level);
                        }

                        // 오버라이드가 있는지 먼저 확인 후 없으면 글로벌 룰 적용
                        var costRule = _globalCost;
                        if (_costOverrides.TryGetValue(kartId, out var ovCost)) costRule = ovCost;

                        // 이미 MaxLevel이면 0 전달. 애초에 호출되지 않도록 할 필요 있음
                        var moneyType = ParseMoneyType(costRule.MoneyType);
                        if (costRule.MaxLevel > 0 && level >= costRule.MaxLevel)
                        {
                            onSuccess?.Invoke(0, moneyType);
                            Debug.LogWarning($"[PatchService] 이미 최대 레벨인 카트{kartId}의 비용을 조회하였습니다");
                            return;
                        }

                        // 최종 가격 계산
                        int cost = ComputeCost(costRule, level);
                        onSuccess?.Invoke(cost, moneyType);
                    },
                    err => onError?.Invoke(err)
                );
        }

        public void GetCostOfUnimo(int unimoId, Action<int, MoneyType> onSuccess, Action<string> onError = null)
        {
            if (!_patchReady) 
            {
                onError?.Invoke("[PatchService] 패치를 불러오지 못하여 가격을 반환할 수 없습니다"); 
                return; 
            }

            var auth = FirebaseManager.Instance?.Auth?.CurrentUser;
            if (auth == null)
            { 
                onError?.Invoke("[PatchService] 유저가 null입니다"); 
                return; 
            }

            DatabaseManager.Instance.GetOnMain(
                DBRoutes.UnimoInventory(auth.UserId, unimoId),
                snap =>
                {
                    int owned = 0;
                    if (snap != null && snap.Exists && snap.Value != null)
                        int.TryParse(snap.Value.ToString(), out owned);

                    // 오버라이드가 있는지 먼저 확인 후 없으면 글로벌 룰 적용
                    var rule = _globalUnimoCost;
                    if (_unimoOverrides.TryGetValue(unimoId, out var ovr)) rule = ovr;

                    var money = ParseMoneyType(rule.moneyType);

                    // 이미 갖고 있으면 0원 전달, 애초에 호출되지 않도록 할 필요 있음
                    onSuccess?.Invoke(owned >= 1 ? 0 : rule.cost, money);
                },
                err => onError?.Invoke(err)
            );
        }

        /// <summary>
        /// 패치와 관련된 DB를 모두 삭제하는 메서드입니다.
        /// 모든 패치 사항이 사라져 정상적인 진행이 되지 않을 수 있으니 호출에 유의해야 합니다.
        /// </summary>
        public void RemoveAllOfPatch()
        {
            if (FirebaseManager.Instance?.IsReady != true)
            {
                Debug.LogError("[PatchToDBUpdater] Firebase가 준비되지 않았습니다.");
                return;
            }

            DatabaseManager.Instance.RemoveOnMain(
                DBRoutes.PatchRoot(),
                onSuccess: () =>
                {
                    Debug.Log("[PatchToDBUpdater] /patch 전체 삭제 완료");
                },
                onError: err =>
                {
                    Debug.LogError($"[PatchToDBUpdater] /patch 삭제 실패: {err}");
                }
            );
        }

        #endregion


        #region Privates

        private struct SpeedRuleDTO
        {
            public SpeedCurveType CurveType;
            public float BaseValue;
            public float LinearStep;
            public float MultiplierStep;
            public int MaxLevel;
            public List<float> Table;
        }

        private struct CostRuleDTO
        {
            public CostGrowthType GrowthType;
            public string MoneyType;
            public int BaseCost;
            public float GrowthRate;
            public int Step;
            public int MaxLevel;
            public List<int> Table;
        }

        private struct UnimoCostDTO
        {
            public string moneyType;
            public int cost;
        }

        private float ComputeSpeed(SpeedRuleDTO rule, int lv)
        {
            switch (rule.CurveType)
            {
                // LinearStep * (레벨 - 1)
                case SpeedCurveType.Linear:
                    return rule.BaseValue + rule.LinearStep * (lv - 1);

                // BaseValue * (MultiplierStep)^(lv - 1). 예) BaseValue = 100, MultiplierStep = 1.1 일 때, 1레벨: 100, 2레벨 110, 3레벨 121, 4레벨 133.1...
                case SpeedCurveType.Multiplier:
                    double step = rule.MultiplierStep <= 0 ? 1.0 : rule.MultiplierStep;
                    return (float)(rule.BaseValue * Math.Pow(step, lv - 1));

                // BaseValue를 무시하고 테이블이 직접 지정. 예) 테이블[0]의 값은 1레벨 속도, 테이블[1]의 값은 2레벨 속도...
                case SpeedCurveType.Table:
                    if (rule.Table == null || rule.Table.Count == 0) return rule.BaseValue;
                    int idx = Mathf.Clamp(lv - 1, 0, rule.Table.Count - 1);
                    return rule.Table[idx];

                default:
                    return rule.BaseValue;
            }
        }

        private int ComputeCost(CostRuleDTO rule, int currentLevel)
        {
            switch (rule.GrowthType)
            {
                // BaseCost + (Step * currentLevel). 코스트는 0에서 1에 대한 계산을 요구할 수 있기 때문에 속도 계산과 다름
                case CostGrowthType.Arithmetic:
                    return rule.BaseCost + rule.Step * currentLevel;

                // BaseCost * (GrowthRate)^(currentLevel). 예) BaseCost = 100, GrowthRate = 2일 때, 0->1레벨: 100, 1->2레벨: 200. 2->3레벨: 400...
                case CostGrowthType.Geometric:
                    double pow = Math.Pow(rule.GrowthRate <= 0 ? 1.0 : rule.GrowthRate, currentLevel);
                    return Mathf.RoundToInt((float)(rule.BaseCost * pow));

                // BaseCost를 무시하고 테이블이 직접 지정. 예) 테이블[0]의 값은 0 -> 1레벨 비용, 테이블[1]의 값은 1 -> 2레벨 비용...
                case CostGrowthType.Table:
                    if (rule.Table == null || rule.Table.Count == 0) return rule.BaseCost;
                    int idx = Mathf.Clamp(currentLevel, 0, rule.Table.Count - 1);
                    return rule.Table[idx];

                default:
                    return rule.BaseCost;
            }
        }


        private SpeedRuleDTO ParseSpeed(DataSnapshot s)
        {
            return new SpeedRuleDTO
            {
                CurveType = ParseCurveType(ReadString(s, DatabaseKeys.curveType)),
                BaseValue = ReadFloat(s, DatabaseKeys.baseValue),
                LinearStep = ReadFloat(s, DatabaseKeys.linearStep),
                MultiplierStep = ReadFloat(s, DatabaseKeys.multiplierStep),
                MaxLevel = ReadInt(s, DatabaseKeys.maxLevel),
                Table = ReadFloatList(s, DatabaseKeys.table)
            };
        }

        private CostRuleDTO ParseCost(DataSnapshot s)
        {
            return new CostRuleDTO
            {
                GrowthType = ParseCostGrowthType(ReadString(s, DatabaseKeys.growthType)),
                MoneyType = ReadString(s, DatabaseKeys.moneyType),
                BaseCost = ReadInt(s, DatabaseKeys.baseCost),
                GrowthRate = ReadFloat(s, DatabaseKeys.growthRate),
                Step = ReadInt(s, DatabaseKeys.step),
                Table = ReadIntList(s, DatabaseKeys.table),
                MaxLevel = ReadInt(s, DatabaseKeys.maxLevel)
            };
        }

        private UnimoCostDTO ParseUnimoCost(DataSnapshot s) => new()
        {
            moneyType = ReadString(s, DatabaseKeys.moneyType),
            cost = ReadInt(s, DatabaseKeys.cost)
        };

        private SpeedCurveType ParseCurveType(string v)
            => Enum.TryParse(v, out SpeedCurveType t) ? t : SpeedCurveType.Linear;

        private CostGrowthType ParseCostGrowthType(string v)
            => Enum.TryParse(v, out CostGrowthType t) ? t : CostGrowthType.Arithmetic;

        private string ReadString(DataSnapshot s, string k)
            => (s != null && s.HasChild(k) && s.Child(k).Value != null) ? s.Child(k).Value.ToString() : "";

        private int ReadInt(DataSnapshot s, string k)
            => (s != null && s.HasChild(k) && s.Child(k).Value != null) ? Convert.ToInt32(s.Child(k).Value) : 0;

        private float ReadFloat(DataSnapshot s, string k)
            => (s != null && s.HasChild(k) && s.Child(k).Value != null) ? Convert.ToSingle(s.Child(k).Value) : 0f;

        private List<float> ReadFloatList(DataSnapshot s, string k)
        {
            var list = new List<float>();
            if (s == null || !s.HasChild(k)) return list;
            foreach (var ch in s.Child(k).Children)
                if (ch.Value != null) list.Add(Convert.ToSingle(ch.Value));
            return list;
        }

        private List<int> ReadIntList(DataSnapshot s, string k)
        {
            var list = new List<int>();
            if (s == null || !s.HasChild(k)) return list;
            foreach (var ch in s.Child(k).Children)
                if (ch.Value != null) list.Add(Convert.ToInt32(ch.Value));
            return list;
        }

        private MoneyType ParseMoneyType(string v)
        {
            if (Enum.TryParse(v, out MoneyType mt)) return mt;
            return default;
        }

        #endregion
    }
}
