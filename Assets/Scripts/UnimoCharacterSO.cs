using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnimoCharacterSO", menuName = "Unimo/Character")]
public class UnimoCharacterSO : ScriptableObject
{
    [Header("ID & 기본 정보")]
    [Tooltip("테이블의 '캐릭터 ID'")]
    public int characterId;

    [Tooltip("테이블의 '캐릭터 이름'")]
    public string characterName;

    [Tooltip("테이블의 '캐릭터 이름'")]
    public GameObject characterPrefab;

    [Tooltip("테이블의 '캐릭터 스프라이트'")]
    public Sprite characterSprite;

    [TextArea]
    [Tooltip("테이블의 '캐릭터 설명'")]
    public string characterInfo;

    [Header("연관/시너지")]
    [Tooltip("테이블의 '시너지 차량 ID'")]
    public int synergyCarId = -1;

    [Tooltip("테이블의 '관계 캐릭터 ID'")]
    public int relationCharacterId = -1;

    [Tooltip("테이블의 '대사 ID'")]
    public int dialogId = -1;
}
