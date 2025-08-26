using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string characterID; // ĳ������ ���̵�
    public string characterName; // ĳ������ �̸�
    public string description; // ����
    public Sprite characterSprite; // ��������Ʈ �̹��� ��ü�� ������ �� �ֽ��ϴ�.
}
