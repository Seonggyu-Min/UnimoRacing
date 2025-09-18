using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemId
{
    None = 0,
    Booster = 30001,    // 부스터
    ThrowBomb,          // 투척용 폭탄 (Thrown/Throwable Bomb 계열)
    Padlock,            // 자물쇠 (락/구속 트랩)
    Shield,             // 실드
    SmokeScreen,        // 시야차단 (연막/잉크 등)
    Missile,            // 미사일 (유도/직선형 모두 커버)
    FriedEgg            // 계란후라이 (바나나 대체 트랩 느낌)
}

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

    [Tooltip("아이템 효과 순서데로 적용 여부")]
    public bool isOptionApplyStep = false;

    public List<StatusEffectOption> options;
}
