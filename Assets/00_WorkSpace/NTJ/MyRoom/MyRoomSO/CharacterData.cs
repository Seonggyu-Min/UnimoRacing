using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string characterID; // 캐릭터의 아이디
    public string characterName; // 캐릭터의 이름
    public string description; // 설명
    public Sprite characterSprite; // 스프라이트 이미지 자체를 저장할 수 있습니다.
}
