using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public enum SpeedCurveType
    {
        Linear,     // 속도가 선형적으로 오르는 방식
        Multiplier, // 속도가 배율로 오르는 방식
        Table       // 테이블로 직접 지정
    }

    public enum CostGrowthType
    {
        Arithmetic, // 등차수열 방식
        Geometric,  // 등비수열 방식
        Table       // 테이블로 직접 지정
    }

    [CreateAssetMenu(fileName = "PatchSO", menuName = "ScriptableObjects/PatchSO")]
    /// <summary>
    /// 카트 업그레이드 관련 패치할 내용이 들어있는 SO입니다.
    /// </summary>
    public class PatchSO : ScriptableObject
    {
        public Globals GlobalRule;                // 오버라이드가 켜지지 않은 모든 카트에 적용
        public List<KartEntry> LocalRuleKarts;    // 오버라이드 룰을 설정할 카트 목록
        public List<UnimoEntry> LocalRuleUnimos;  // 오버라이드 룰을 설정할 유니모 목록


        [Serializable]
        public class Globals // 글로벌 설정
        {
            public SpeedRule SpeedRule;          // 카트 스피드 상승 규칙 설정
            public KartCostRule KartCostRule;    // 카트   비용 상승 규칙 설정
            public UnimoCostRule UnimoCostRule;  // 유니모 비용 상승 규칙 설정
        }

        [Serializable]
        public class KartEntry
        {
            public int KartId;                      // 카트 인덱스
            public bool SpeedOverride = false;      // 속도 상승에 있어 글로벌 룰을 따르지 않을 것인지 결정
            public bool CostOverride = false;       // 비용 상승에 있어 글로벌 룰을 따르지 않을 것인지 결정
            public SpeedRule SpeedRule;             // override일 때만 유효
            public KartCostRule KartCostRule;       // override일 때만 유효
        }

        [Serializable]
        public class SpeedRule
        {
            public SpeedCurveType CurveType;    // 속도 상승 방식을 설정
            public int MaxLevel;                // 최대 강화 레벨 설정
            public float BaseValue;             // 초기 속도
            public float LinearStep;            // 선형적 방식에서 한 레벨 당 얼만큼 속도를 올릴지 설정 (덧셈)
            public float MultiplierStep;        // 배율 방식에서   한 레벨 당 얼만큼 속도를 올릴지 설정 (곱셈, 1 이상이 아니면 줄어듦)
            public List<float> Table;           // 테이블 방식일 때만 사용
        }

        [Serializable]
        public class KartCostRule
        {
            public CostGrowthType GrowthType;   // 비용 상승 방식 설정
            public MoneyType MoneyType;         // 어떤 재화를 소모할 지 설정
            public int BaseCost;                // 최초 강화 값
            public int MaxLevel;                // 최대 강화 레벨 설정
            public float GrowthRate;            // 등비수열 방식일 때만 사용
            public int Step;                    // 등차수열 방식일 때만 사용
            public List<int> Table;             // 테이블 방식일 때만 사용
        }

        [Serializable]
        public class UnimoCostRule
        {
            public int Cost;
            public MoneyType MoneyType;         // 어떤 재화를 소모할 지 설정
        }

        [Serializable]
        public class UnimoEntry
        {
            public int UnimoId;
            public int Cost;
            public MoneyType MoneyType;         // 어떤 재화를 소모할 지 설정
        }
    }
}