using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string characterID;
    public string characterName;
    public string description;
    public Sprite characterSprite;
    public bool isOwned; // 보유 여부 변수 추가
}
