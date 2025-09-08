using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rules")]
public class InGameRaceRulesConfig : ScriptableObject
{
    [Header("Flow")]
    public int laps = 3;
    public int countdownSeconds = 3;
    public float finishSeconds = 10f;
    public float postGameSeconds = 10f;

    [Header("Players")]
    public int playablePlayersCount = 4;

    [Header("Items")]
    public bool itemsEnabled = true;
}