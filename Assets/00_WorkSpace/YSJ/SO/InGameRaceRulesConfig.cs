using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rules")]
public class InGameRaceRulesConfig : ScriptableObject
{
    [Header("Flow")]
    public int laps = 3;
    public float countdownSeconds = 3f;
    public float finishSeconds = 10f;
    public float postGameSeconds = 10f;
    public float timeLimitSeconds = 0f; // 0=무제한

    [Header("Players")]
    public int playablePlayersCount = 4;

    [Header("Items")]
    public bool itemsEnabled = true;
}