using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGameItemSpawnProbabilitySO", menuName = "Game/ItemSpawnProbability")]
public class GameItemSpawnProbabilitySO : ScriptableObject
{
    public List<ItemSpawnProbabilityData> probabilityList = new();
}