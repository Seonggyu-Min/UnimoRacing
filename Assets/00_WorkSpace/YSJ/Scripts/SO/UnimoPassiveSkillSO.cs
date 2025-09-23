using UnityEngine;

public enum PassiveSkillType
{
    None,
    ConditionalEnhancement, // 조건부 강화 > 효과 처리
    Creation,               // 아이템 생성 > 아이템 추가
    Defense,                // 아이템 방어 > 효과 처리 무시
    Enhancement,            // 아이템 강화 > 아이템 변경
}

public enum TriggerCondition
{
    None,
    Collsion,   // 충돌 시 체크
    PickUp,     // 획득 시 체크
}

[CreateAssetMenu(fileName = "NewUnimoPassiveSkillSO", menuName = "Unimo/PassiveSkill")]
public class UnimoPassiveSkillSO : ScriptableObject
{
    [Header("ID & 기본 정보")]
    [Tooltip("테이블의 '패시브 스킬 ID'")]
    public int passiveSkillID;

    [Tooltip("테이블의 '패시브 스킬 이름'")]
    public string passiveSkillName;

    [Tooltip("패시브 스킬 아이콘 이미지")]
    public Sprite passiveSkillIconSprite;

    [Tooltip("테이블의 '스킬 타입'")]
    public PassiveSkillType passiveSkillType = PassiveSkillType.None;

    [Header("About > Trigger Info")]
    public TriggerCondition triggerCondition = TriggerCondition.None;
    public ItemId triggerItemID = ItemId.None;
    public int triggerCount = -1;

    public ItemId triggerRewardItemID = ItemId.None;
}