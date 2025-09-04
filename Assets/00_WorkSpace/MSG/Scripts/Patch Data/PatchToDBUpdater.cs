using EditorAttributes;
using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    // TODO: UID 검증, 에디터에서만 기능 등 보안 검증 로직 추가 예정
#if UNITY_EDITOR
    public class PatchToDBUpdater : MonoBehaviour
    {
        [SerializeField] private PatchSO _patchSO;

        // _patchSO를 기반으로 Firebase DB에 업데이트
        [Button("Update DB", 50f)]
        public void OnClickUpdate()
        {
            if (_patchSO == null)
            {
                Debug.LogError("[PatchToDBUpdater] PatchSO가 null입니다");
                return;
            }

            if (_patchSO.GlobalRule.SpeedRule == null || _patchSO.GlobalRule.KartCostRule == null)
            {
                Debug.LogError("[PatchToDBUpdater] PatchSO의 정보가 비어있습니다");
                return;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var body = BuildPatchTree(_patchSO);
            body[DatabaseKeys.updatedAt] = now; // 패치 시각 기록

            DatabaseManager.Instance.RunTransactionOnMain(
                DBRoutes.PatchRoot(),
                mutable =>
                {
                    int currentVersion = 0;
                    if (mutable.HasChild(DatabaseKeys.version) && mutable.Child(DatabaseKeys.version).Value != null)
                    {
                        int.TryParse(mutable.Child(DatabaseKeys.version).Value.ToString(), out currentVersion);
                    }

                    int newVersion = currentVersion + 1;
                    body[DatabaseKeys.version] = newVersion;

                    // 전체 교체
                    mutable.Value = body;
                    return TransactionResult.Success(mutable);
                },
                snap => Debug.Log("[PatchToDBUpdater] 패치 업로드 성공"),
                err => Debug.LogError($"[PatchToDBUpdater] 패치 업로드 실패: {err}")
            );
        }

        #region Private Methods

        // SO를 DB의 구조로 변환
        private Dictionary<string, object> BuildPatchTree(PatchSO so)
        {
            Dictionary<string, object> root = new();

            // globals 설정
            Dictionary<string, object> globals = new()
            {
                [DatabaseKeys.speed] = MapSpeed(so.GlobalRule.SpeedRule),
                [DatabaseKeys.cost] = MapCost(so.GlobalRule.KartCostRule),
                [DatabaseKeys.unimoCost] = MapUnimoCost(so.GlobalRule.UnimoCostRule)
            };

            // karts
            Dictionary<string, object> karts = new();
            if (so.LocalRuleKarts != null)
            {
                foreach (var e in so.LocalRuleKarts)
                {
                    Dictionary<string, object> node = new()
                    {
                        [DatabaseKeys.speedOverride] = e.SpeedOverride,
                        [DatabaseKeys.costOverride] = e.CostOverride
                    };

                    if (e.SpeedOverride && e.SpeedRule != null)
                    {
                        node[DatabaseKeys.speed] = MapSpeed(e.SpeedRule);
                    }

                    if (e.CostOverride && e.KartCostRule != null)
                    {
                        node[DatabaseKeys.cost] = MapCost(e.KartCostRule);
                    }

                    karts[e.KartId.ToString()] = node;
                }
            }

            Dictionary<string, object> unimos = new();
            if (so.LocalRuleUnimos != null)
            {
                foreach (var u in so.LocalRuleUnimos)
                {
                    Dictionary<string, object> node = new()
                    {
                        [DatabaseKeys.costOverride] = u.CostOverride
                    };

                    if (u.CostOverride)
                    {
                        node[DatabaseKeys.moneyType] = u.MoneyType.ToString();
                        node[DatabaseKeys.cost] = u.Cost;
                    }

                    unimos[u.UnimoId.ToString()] = node;
                }
            }

            root[DatabaseKeys.unimos] = unimos;
            root[DatabaseKeys.globals] = globals;
            root[DatabaseKeys.karts] = karts;
            return root;
        }

        private Dictionary<string, object> MapSpeed(PatchSO.SpeedRule r)
        {
            Dictionary<string, object> mapDict = new()
            {
                [DatabaseKeys.curveType] = r.CurveType.ToString(),
                [DatabaseKeys.baseValue] = r.BaseValue,
                [DatabaseKeys.linearStep] = r.LinearStep,
                [DatabaseKeys.multiplierStep] = r.MultiplierStep,
                [DatabaseKeys.maxLevel] = r.MaxLevel
            };
            if (r.Table != null && r.Table.Count > 0)
            {
                mapDict[DatabaseKeys.table] = new List<object>(r.Table.ConvertAll(x => (object)x));
            }
            return mapDict;
        }

        private Dictionary<string, object> MapCost(PatchSO.KartCostRule c)
        {
            Dictionary<string, object> costDict = new()
            {
                [DatabaseKeys.growthType] = c.GrowthType.ToString(),
                [DatabaseKeys.moneyType] = c.MoneyType.ToString(),
                [DatabaseKeys.baseCost] = c.BaseCost,
                [DatabaseKeys.growthRate] = c.GrowthRate,
                [DatabaseKeys.step] = c.Step,
                [DatabaseKeys.maxLevel] = c.MaxLevel
            };
            if (c.Table != null && c.Table.Count > 0)
            {
                costDict[DatabaseKeys.table] = new List<object>(c.Table.ConvertAll(x => (object)x));
            }
            return costDict;
        }

        private Dictionary<string, object> MapUnimoCost(PatchSO.UnimoCostRule u)
        {
            return new Dictionary<string, object>
            {
                [DatabaseKeys.moneyType] = u.MoneyType.ToString(),
                [DatabaseKeys.cost] = u.Cost
            };
        }

        #endregion
    }
#endif
}
