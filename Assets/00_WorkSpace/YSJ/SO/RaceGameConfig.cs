using UnityEngine;

[CreateAssetMenu(fileName = "RaceGameConfig", menuName = "Game/RaceGameConfig")]
public sealed class RaceGameConfig : ScriptableObject
{
    [Min(2)] public int RaceMinPlayer = 2;          // �� �ּ� �ο�
    [Min(2)] public int RaceMaxPlayer = 4;          // �� �ִ� �ο�
    [Min(1)] public int RaceChoosableMapCount = 3;  // ���̽� ���� ������ �� ��

    public static RaceGameConfig Load()
        => Resources.Load<RaceGameConfig>("Config/RaceGameConfig");
}
