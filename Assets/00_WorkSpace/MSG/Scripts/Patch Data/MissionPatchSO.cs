using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    /// <summary>
    /// 데일리 미션, 도전과제 미션을 구분합니다.
    /// </summary>
    public enum MissionGroup
    {
        Daily,
        Achievement
    }

    /// <summary>
    /// 미션의 타입.
    /// 코드에서 미션 종류의 판단용으로 사용하지는 않습니다.
    /// </summary>
    public enum MissionType
    {
        RaceFinish,     // 완주 여부
        Change,         // 유니모 및 엔진 교체
        Item,           // 아이템 획득 및 사용
        Collect,        // 유니모 및 엔진 수집
        Obtain          // 재화 획득
    }

    // ========== 미션 조립을 위한 동사, 목적어, 파티 유무입니다. ==========
    // 인덱스를 통해 미션이 늘어날 때마다 코드를 고치는 것이 아닌
    // 미션 정의 테이블을 기준으로 매칭되는 모든 미션에 대해 진행도를 증가시기기 위해
    // 동사, 목적어, 파티 유무를 조립하여 사용하도록 하였습니다.
    // 예) MissionVerb.Finish, MissionObject.Race, PartyCondition.True
    // => 파티와 함께 레이스를 완주하는 미션
    // =====================================================================

    /// <summary>
    /// 미션의 진행도 증가를 트리거하는 동사입니다.
    /// </summary>
    public enum MissionVerb
    {
        Obtain,         // 획득
        Use,            // 사용
        Finish,         // 완주
        Collect,        // 수집
        Change,         // 교체
    }

    /// <summary>
    /// 미션의 진행도 증가를 트리거하는 목적어입니다.
    /// </summary>
    public enum MissionObject
    {
        Race,           // 레이스
        Engine,         // 엔진
        Unimo,          // 유니모
        Item,           // 아이템(인게임에서 사용하는 아이템)
        Gold,           // 골드
        BluyHoneyGem,   // 블루허니잼
    }

    /// <summary>
    /// 미션의 진행도 증가를 트리거할 때 파티 유무 구분용입니다.
    /// </summary>
    public enum PartyCondition
    {
        Any,            // 파티 유무와 관계 없는 미션
        True,           // 파티와 함께 진행해야 하는 미션
        False,          // 솔로로 진행해야 하는 미션
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
        public MissionVerb MissionVerb;
        public MissionObject MissionObject;
        public PartyCondition PartyCondition;
        //public string SubKey;                 // 특정 아이템, 맵, 카트 등을 지정해야된다면 필요할 듯.
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
