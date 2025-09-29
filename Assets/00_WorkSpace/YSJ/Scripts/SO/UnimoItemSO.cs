using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnimoItemSO", menuName = "Unimo/Item")]
public class UnimoItemSO : ScriptableObject
{
    [Header("ID & 기본 정보")]
    [Tooltip("테이블의 '아이템 ID'")]
    public ItemId itemID = ItemId.None;

    [Tooltip("테이블의 '아이템 이름'")]
    public string itemName = "NoName";

    [Tooltip("아이템 아이콘 이미지")]
    public Sprite itemIconSprite;

    [Tooltip("아이템 저장 가능 상태 여부")]
    public bool isInventorySavable = false;

    // =====================================================================

    [Header("효과 관련 설정")]
    [Tooltip("효과 재적용 모드")]
    public ReapplyMode reapplyMode = ReapplyMode.RefreshDuration;

    [Tooltip("효과 지속 시간")]
    public float itemEffectDuration;

    [Tooltip("효과 최대 스택 수"), Min(1)]
    public float stackCount = 1;

    public List<StatusEffectOption> options;
}
