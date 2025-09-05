using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnimoKartSO", menuName = "Unimo/Kart")]
public class UnimoKartSO : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("차량 ID")]
    public int carId;

    [Tooltip("차량 이름")]
    public string carName;

    [Tooltip("차량 프리팹")]
    public GameObject kartPrefab;

    [TextArea]
    [Tooltip("차량 설명")]
    public string carDesc;

    [Header("스킬")]
    [Tooltip("패시브 스킬 ID")]
    public int passiveSkillId;
}
