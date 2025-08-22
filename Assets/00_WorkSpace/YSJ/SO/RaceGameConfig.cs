using UnityEngine;

[CreateAssetMenu(fileName = "RaceGameConfig", menuName = "Game/RaceGameConfig")]
public sealed class RaceGameConfig : ScriptableObject
{
    [Min(2)] public int RaceMinPlayer = 2;          // 방 최소 인원
    [Min(2)] public int RaceMaxPlayer = 4;          // 방 최대 인원
    [Min(1)] public int RaceChoosableMapCount = 3;  // 레이싱 선택 가능한 맵 수

    public static RaceGameConfig Load()
        => Resources.Load<RaceGameConfig>("Config/RaceGameConfig");
}
