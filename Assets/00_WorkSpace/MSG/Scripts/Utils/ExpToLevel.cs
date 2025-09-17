using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public static class ExpToLevel
    {
        // 기본 설정값
        private static int _initialExp = 100;
        private static float _growthRate = 1.061f;
        private static int _minLevel = 1;
        private static int _maxLevel = 100;

        /// <summary>현재 곡선의 초기 필요 EXP.</summary>
        public static int InitialExp => _initialExp;

        /// <summary>현재 곡선의 성장률(레벨당 배율).</summary>
        public static float GrowthRate => _growthRate;

        /// <summary>최소 레벨(보통 1).</summary>
        public static int MinLevel => _minLevel;

        /// <summary>최대 레벨(레벨 캡).</summary>
        public static int MaxLevel => _maxLevel;


        /// <summary>
        /// 해당 레벨에서 다음 레벨까지 필요한 EXP를 반환합니다.
        /// </summary>
        /// <param name="level">현재 레벨.</param>
        /// <returns>다음 레벨까지 필요한 EXP(내림 반영).</returns>
        public static int ExpToNextLevel(int level)
        {
            level = Mathf.Clamp(level, _minLevel, _maxLevel);
            double need = _initialExp * Math.Pow(_growthRate, level - 1);
            int needInt = Mathf.Max(1, Mathf.FloorToInt((float)need));
            return needInt;
        }

        /// <summary>
        /// 특정 레벨의 시작 시점까지 필요한 총 누적 EXP를 반환합니다.
        /// <para>예: 레벨 1 시작 = 0, 레벨 2 시작 = L1 필요치 합, 레벨 3 시작 = L1+L2 필요치 합...</para>
        /// </summary>
        /// <param name="level">대상 레벨.</param>
        /// <returns>레벨 시작까지의 누적 EXP.</returns>
        public static int TotalExpAtLevelStart(int level)
        {
            // level <= min -> 0
            if (level <= _minLevel) return 0;

            level = Mathf.Min(level, _maxLevel + 1);
            int total = 0;
            for (int l = _minLevel; l < level; l++)
                total += ExpToNextLevel(l);
            return total;
        }

        /// <summary>
        /// 누적 EXP(총 경험치)로부터 현재 레벨을 계산합니다.
        /// </summary>
        /// <param name="totalExp">플레이어의 총 누적 EXP.</param>
        /// <returns>현재 레벨(최소~최대 범위 내).</returns>
        public static int LevelFromTotalExp(int totalExp)
        {
            if (totalExp <= 0) return _minLevel;

            int cum = 0;
            for (int l = _minLevel; l <= _maxLevel; l++)
            {
                int need = ExpToNextLevel(l);
                if (totalExp < cum + need)
                    return l;

                cum += need;
            }
            return _maxLevel;
        }

        /// <summary>
        /// 누적 EXP 기준으로 현재 레벨에서의 진행도(0~1)를 반환합니다.
        /// <para>UI의 게이지 <see cref="Image.fillAmount"/> 등에 바로 사용할 수 있습니다.</para>
        /// </summary>
        /// <param name="totalExp">플레이어의 총 누적 EXP.</param>
        /// <returns>현재 레벨 진행도(0~1). 최대 레벨이면 1.</returns>
        public static float Fill01FromTotalExp(int totalExp)
        {
            int level = LevelFromTotalExp(totalExp);
            if (level >= _maxLevel) return 1f;

            int start = TotalExpAtLevelStart(level);
            int need = ExpToNextLevel(level);
            int prog = Mathf.Clamp(totalExp - start, 0, need);
            return Mathf.Clamp01(need > 0 ? (float)prog / need : 1f);
        }

        /// <summary>
        /// 현재 누적 EXP에서 레벨 내 진행 수치/요구치 등의 상세 정보를 반환합니다.
        /// </summary>
        /// <param name="totalExp">플레이어의 총 누적 EXP.</param>
        /// <param name="level">계산된 현재 레벨.</param>
        /// <param name="inLevelProgress">현재 레벨 내 누적된 EXP.</param>
        /// <param name="inLevelNeed">현재 레벨 내 다음 레벨까지 필요한 총 EXP.</param>
        public static void GetProgressDetail(int totalExp, out int level, out int inLevelProgress, out int inLevelNeed)
        {
            level = LevelFromTotalExp(totalExp);
            if (level >= _maxLevel)
            {
                inLevelNeed = 1;
                inLevelProgress = 1;
                return;
            }

            int start = TotalExpAtLevelStart(level);
            inLevelNeed = ExpToNextLevel(level);
            inLevelProgress = Mathf.Clamp(totalExp - start, 0, inLevelNeed);
        }
    }
}
