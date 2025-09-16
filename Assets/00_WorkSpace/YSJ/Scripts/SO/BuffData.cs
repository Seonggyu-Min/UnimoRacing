using UnityEngine;

[CreateAssetMenu(fileName = "BuffData", menuName = "Game/Buff Data")]
public class BuffData : ScriptableObject
{
    [Header("기본 정보")]
    public BuffId id;
    public BuffCategory category;
    public BuffStackPolicy stackPolicy;

    [Header("버프 지속 시간 (초)")]
    public float baseDuration = 3.0f;

    [Header("아이콘/이펙트")]
    public Sprite icon;
    public GameObject vfxPrefab;
}
