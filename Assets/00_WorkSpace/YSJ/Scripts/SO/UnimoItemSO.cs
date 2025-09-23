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

    [Tooltip("아이템 등장 가중치"), Min(0.0f)]
    public float _spawnWeight = 1.0f;

    [Header("효과 관련 설정")]
    [Tooltip("아이템 효과 적용 시, 효과 초기화(false 시, 해당 아이템 상태가 완전히 끝났을 때 적용됩니다. 스택 수가 무시됩니다.)")]
    public bool isReapplication = false;

    [Tooltip("아이템 효과 최대 스택 수")]
    public float stackCount = 1;

    public List<StatusEffectOption> options;
}
