using UnityEngine;

[CreateAssetMenu(fileName = "RaceGameConfigSO", menuName = "Game/RaceGameConfigSO")]
public sealed class MatchingConfig : ScriptableObject
{
    [Min(2)] public int RoomRacePlayer          = 4;    // 방 최대 인원
    [Min(1)] public int RaceChoosableMapCount   = 3;    // 레이싱 선택 가능한 맵 수

    public static MatchingConfig Load()
        => Resources.Load<MatchingConfig>("SO/Config/RaceGameConfigSO");
}
