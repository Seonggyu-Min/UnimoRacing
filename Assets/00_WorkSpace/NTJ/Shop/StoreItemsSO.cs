using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StoreItems", menuName = "ScriptableObjects/StoreItems")]
public class StoreItemsSO : ScriptableObject
{
    // ��� īƮ SO�� ���ϸ� SO�� ���� ����Ʈ
    public List<UnimoKartSO> karts;
    public List<UnimoCharacterSO> unimos;
}
