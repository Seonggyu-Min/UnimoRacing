using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAudioDB", menuName = "Audio/Audio Database")]
public class AudioDB : ScriptableObject
{
    public List<AudioData> AudioDataList = new List<AudioData>();
}
