using System;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
[CreateAssetMenu(fileName = "NewAudioData", menuName = "Audio/Audio Data")]
public class AudioData : ScriptableObject
{
    [Header("���� ����")]
    public string ClipName;
    // ���� AudioClip Clip; ������ Addressable �ּҸ� �޴� ���ڿ��� �����մϴ�.
    public string ClipAddress;


    [Header("���� ����")]
    [Range(0f, 1f)] public float Volume = 1.0f;
    [Range(0.1f, 3f)] public float Pitch = 1.0f;
    public bool Loop = false;
    public AudioMixerGroup MixerGroup;

    // [NonSerialized] �Ӽ��� �� �ʵ尡 ������� �ʰ�, ���� ���� �߿��� ������ �ǹ��մϴ�.
    [NonSerialized]
    public AudioClip Clip;



#if UNITY_EDITOR
    // �ν����Ϳ��� ���� ����� ������ ȣ��Ǵ� ����Ƽ ������ ���� �Լ��Դϴ�.
    private void OnValidate()
    {
        // ClipName�� ���������, �� ScriptableObject ������ ���ϸ��� �⺻������ ����մϴ�.
        if (string.IsNullOrWhiteSpace(ClipName))
        {
            ClipName = this.name;
        }
    }
#endif
}
