using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public enum SkillType
{
    None,
    ConditionalEnhancement, // 조건부 강화
    Creation,               // 아이템 생성
    Defense,                // 아이템 방어
    Enhancement,            // 아이템 강화
}

public enum TriggerType
{
    None,
    Collect,                // 컬랙트
    Collision,              // 콜리션
    PickUp,                 // 획득
}



[CreateAssetMenu(fileName = "NewUnimoSkillSO", menuName = "Unimo/Skill")]
public class UnimoSkillSO : ScriptableObject
{
    [Header("ID & 기본 정보")]
    [Tooltip("테이블의 '스킬 ID'")]
    public int skillID;

    [Tooltip("테이블의 '스킬 이름'")]
    public string skillName;

    [Tooltip("스킬 아이콘 이미지")]
    public Sprite skillIconSprite;

    [Tooltip("스킬 오브젝트 프리팹")]
    public GameObject skillPrefab;

    [Tooltip("테이블의 '스킬 타입'")]
    public SkillType skillType = SkillType.None;

    [Tooltip("테이블의 '발동 조건'")]
    public TriggerType triggerType = TriggerType.None;

    [Tooltip("테이블의 '연관 아이템'")]
    public int skillItemID = -1;

    [Header("Trigger Type > Collect")]
    [Tooltip("테이블의 '수집형 아이템'")]
    public int collectCount = -1;

    [Header("Options")]
    [Tooltip("테이블의 '사용시 적용 옵션'")]
    public List<SkillOption> options;
}