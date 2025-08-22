using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "NewAudioData", menuName = "Audio/Audio Data")]
public class AudioData : ScriptableObject
{
    [Header("사운드 정보")]
    public string ClipName;
    public AudioClip Clip;

    [Header("사운드 설정")]
    [Range(0f, 1f)] public float Volume = 1.0f;
    [Range(0.1f, 3f)] public float Pitch = 1.0f;
    public bool Loop = false;
    public AudioMixerGroup MixerGroup;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // ClipName이 비어있으면 에셋 파일명을 기본값으로 사용
        if (string.IsNullOrWhiteSpace(ClipName))
        {
            ClipName = name;
        }

        if (Clip == null)
        {
            Debug.LogWarning($"[AudioData] '{ClipName}'에 AudioClip이 할당되지 않았습니다.", this);
        }
    }
#endif
}
