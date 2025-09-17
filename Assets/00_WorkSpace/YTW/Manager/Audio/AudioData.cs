using System;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
[CreateAssetMenu(fileName = "NewAudioData", menuName = "Audio/Audio Data")]
public class AudioData : ScriptableObject
{
    [Header("사운드 정보")]
    public string ClipName;
    // 기존 AudioClip Clip; 변수를 Addressable 주소를 받는 문자열로 변경합니다.
    public string ClipAddress;


    [Header("사운드 설정")]
    [Range(0f, 1f)] public float Volume = 1.0f;
    [Range(0.1f, 3f)] public float Pitch = 1.0f;
    public bool Loop = false;
    public AudioMixerGroup MixerGroup;

    // [NonSerialized] 속성은 이 필드가 저장되지 않고, 오직 실행 중에만 사용됨을 의미합니다.
    [NonSerialized]
    public AudioClip Clip;



#if UNITY_EDITOR
    // 인스펙터에서 값이 변경될 때마다 호출되는 유니티 에디터 전용 함수입니다.
    private void OnValidate()
    {
        // ClipName이 비어있으면, 이 ScriptableObject 에셋의 파일명을 기본값으로 사용합니다.
        if (string.IsNullOrWhiteSpace(ClipName))
        {
            ClipName = this.name;
        }
    }
#endif
}
