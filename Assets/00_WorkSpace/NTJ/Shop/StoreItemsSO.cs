using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StoreItems", menuName = "ScriptableObjects/StoreItems")]
public class StoreItemsSO : ScriptableObject
{
    // 모든 카트 SO와 유니모 SO를 담을 리스트
    public List<UnimoKartSO> karts;
    public List<UnimoCharacterSO> unimos;
}
