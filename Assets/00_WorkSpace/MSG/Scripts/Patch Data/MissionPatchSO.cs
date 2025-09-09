using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public enum MissionGroup
    {
        Daily,
        Achievement
    }

    public enum MissionType
    {
        RaceFinish,     // 완주 여부
        Change,         // 유니모 및 엔진 교체
        Item,           // 아이템 획득 및 사용
        Collect,        // 유니모 및 엔진 수집
        Obtain          // 재화 획득
    }

    [Serializable]
    public class MissionEntry
    {
        public int Index;
        public string Title;
        public MissionType MissionType;
        [TextArea] public string Description;
        public MoneyType MoneyType;
        public int RewardQuantity;
        public int TargetCount;
    }

    [CreateAssetMenu(fileName = "MissionPatchSO", menuName = "ScriptableObjects/MissionPatchSO")]
    public class MissionPatchSO : ScriptableObject
    {
        [Header("Dailies")]
        public List<MissionEntry> Dailies = new();

        [Header("Achievements")]
        public List<MissionEntry> Achievements = new();
    }
}
